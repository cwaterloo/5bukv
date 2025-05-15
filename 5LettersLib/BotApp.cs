using Microsoft.Extensions.DependencyInjection;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;
using FiveLetters.Data;
using System.Text;
using Protobuf.Text;
using Telegram.BotAPI.UpdatingMessages;
using System.Resources;
using System.Globalization;

namespace FiveLetters
{
    internal record Msg(string Text, InlineKeyboardMarkup Markup);

    internal enum SessionStatus
    {
        InProgress,
        Completed,
        Error
    }

    public sealed class BotApp(TelegramBotClient client, Tree tree, CultureInfo cultureInfo, ResourceManager resourceManager)
    {
        private string GetResourceString(string key) {
            return resourceManager.GetString(key, cultureInfo)!;
        }

        private static SessionStatus GetSessionStatus(bool noEdges, bool noWordsLeft)
        {
            if (noEdges)
            {
                return SessionStatus.Completed;
            }

            return noWordsLeft ? SessionStatus.Error : SessionStatus.InProgress;
        }

        private string GetSessionStatusText(SessionStatus sessionStatus)
        {
            return sessionStatus switch
            {
                SessionStatus.InProgress => GetResourceString("InProgress"),
                SessionStatus.Completed => GetResourceString("Completed"),
                SessionStatus.Error => GetResourceString("Error"),
                _ => throw new InvalidDataException(string.Format("Unknown enum value: {0}", sessionStatus)),
            };
        }

        private static IEnumerable<Data.Evaluation> GetDefaultEvaluation()
        {
            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                yield return Data.Evaluation.Absent;
            }
        }

        private static GameState MakeInitialGameState()
        {
            return new GameState
            {
                Evaluation = { GetDefaultEvaluation() }
            };
        }

        private static char GetEvaluationChar(Data.Evaluation evaluation)
        {
            return evaluation switch
            {
                Data.Evaluation.Absent => '−',
                Data.Evaluation.Present => '+',
                Data.Evaluation.Correct => '=',
                _ => throw new InvalidDataException(string.Format("Unknown enum value: {0}", evaluation)),
            };
        }

        private static Data.Evaluation Next(Data.Evaluation evaluation)
        {
            return (Data.Evaluation)(((int)evaluation + 1) %
                Enum.GetValues<Data.Evaluation>().Length);
        }

        private static Data.Evaluation Prev(Data.Evaluation evaluation)
        {
            return (Data.Evaluation)(((int)evaluation - 1 +
                Enum.GetValues<Data.Evaluation>().Length) % Enum.GetValues<Data.Evaluation>().Length);
        }

        private static string ToText(char letter, Data.Evaluation evaluation)
        {
            return string.Format("{0}{1}", GetEvaluationChar(evaluation), letter);
        }

        private Msg? MakeMsg(GameState gameState)
        {
            if (gameState.Evaluation.Count != Word.WordLetterCount)
            {
                return null;
            }

            bool noWordsLeft = false;
            Tree lastTree = tree;
            List<string> wordChain = [];
            wordChain.Add(lastTree.Word);
            foreach (int state in gameState.Chain)
            {
                if (lastTree.Edges.TryGetValue(state, out Tree subtree))
                {
                    lastTree = subtree;
                    wordChain.Add(lastTree.Word);
                }
                else
                {
                    noWordsLeft = true;
                    wordChain.Add(string.Format(cultureInfo, "\\[{0}\\]", GetResourceString("NoSuitableWords")));
                    break;
                }
            }

            SessionStatus sessionStatus = GetSessionStatus(lastTree.Edges.Count == 0, noWordsLeft);

            List<List<InlineKeyboardButton>> buttons = [];
            List<InlineKeyboardButton> buttonRowOne = [];
            List<InlineKeyboardButton> buttonRowTwo = [];

            StringBuilder textBuilder = new();
            switch (sessionStatus)
            {
                case SessionStatus.Completed:
                    textBuilder.Append(string.Format(cultureInfo, GetResourceString("WordTemplate"), lastTree.Word));
                    break;
                case SessionStatus.InProgress:
                    textBuilder.Append(string.Format(cultureInfo, GetResourceString("SuggestionTemplate"), lastTree.Word));
                    break;
            }
            textBuilder.Append(string.Format(cultureInfo, GetResourceString("WordChainTemplate"), string.Join(" \\-\\> ", wordChain)));
            textBuilder.Append(string.Format(cultureInfo, GetResourceString("StateTemplate"), GetSessionStatusText(sessionStatus)));
            textBuilder.Append(string.Format(cultureInfo, GetResourceString("HelpTemplate"), "/help"));

            if (gameState.Chain.Count > 0)
            {
                // Back button
                GameState prevGameState = new()
                {
                    Chain = { gameState.Chain.SkipLast(1) },
                    Evaluation = { Evaluation.Unpack(gameState.Chain[^1]).ToDataEvaluations() }
                };

                buttonRowTwo.Add(new InlineKeyboardButton(GetResourceString("Back")) { CallbackData = GameStateSerializer.Save(prevGameState) });
            }

            if (lastTree.Edges.Count != 0 && !noWordsLeft)
            {
                // Forward button
                GameState nextGameState = new()
                {
                    Chain = { gameState.Chain.Append(Evaluation.FromDataEvaluations(gameState.Evaluation, lastTree.Word).Pack()) },
                    Evaluation = { GetDefaultEvaluation() }
                };

                buttonRowTwo.Add(new InlineKeyboardButton(GetResourceString("Forward")) { CallbackData = GameStateSerializer.Save(nextGameState) });

                // Letter buttons                
                GameState letterGameState = gameState.Clone();
                for (int i = 0; i < Word.WordLetterCount; ++i)
                {
                    letterGameState.Evaluation[i] = Next(letterGameState.Evaluation[i]);
                    buttonRowOne.Add(new InlineKeyboardButton(ToText(lastTree.Word[i], gameState.Evaluation[i]))
                    { CallbackData = GameStateSerializer.Save(letterGameState) });
                    letterGameState.Evaluation[i] = Prev(letterGameState.Evaluation[i]);
                }
            }

            if (buttonRowOne.Count > 0)
            {
                buttons.Add(buttonRowOne);
            }

            if (buttonRowTwo.Count > 0)
            {
                buttons.Add(buttonRowTwo);
            }

            return new Msg(textBuilder.ToString(), new InlineKeyboardMarkup(buttons));
        }

        private void ProcessStart(long chatId)
        {
            Msg? msg = MakeMsg(MakeInitialGameState());
            if (msg != null)
            {
                try
                {
                    client.SendMessage(chatId, text: msg.Text, replyMarkup: msg.Markup, parseMode: "MarkdownV2");
                }
                catch (BotRequestException ex)
                {
                    if (ex.ErrorCode / 100 != 4)
                    {
                        throw;
                    }
                }
            }
        }

        private void ProcessHelp(long chatId)
        {
            try
            {
                client.SendMessage(chatId, text: GetResourceString("Help"));
            }
            catch (BotRequestException ex)
            {
                if (ex.ErrorCode / 100 != 4)
                {
                    throw;
                }
            }
        }

        private void ProcessMessage(Message message)
        {
            switch (message.Text)
            {
                case "/start":
                    ProcessStart(message.Chat.Id);
                    return;
                case "/help":
                    ProcessHelp(message.Chat.Id);
                    return;
            }
        }

        private void ProcessCallbackQuery(CallbackQuery callbackQuery)
        {
            if (callbackQuery.Data == null || callbackQuery.Message == null)
            {
                return;
            }

            Msg? msg = MakeMsg(GameStateSerializer.Load(callbackQuery.Data));
            if (msg != null)
            {
                try
                {
                    client.EditMessageText(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId,
                        text: msg.Text, replyMarkup: msg.Markup, parseMode: "MarkdownV2");
                }
                catch (BotRequestException ex)
                {
                    if (ex.ErrorCode / 100 != 4)
                    {
                        throw;
                    }
                }
            }
        }

        private void ProcessUpdates(IEnumerable<Update> updates)
        {
            foreach (Update update in updates)
            {
                if (update.Message != null)
                {
                    ProcessMessage(update.Message);
                }
                else if (update.CallbackQuery != null)
                {
                    ProcessCallbackQuery(update.CallbackQuery);
                }
            }
        }

        private void ProcessUpdates()
        {
            int? lastUpdateId = null;
            do
            {
                IEnumerable<Update> updates = lastUpdateId.HasValue ?
                    client.GetUpdates(lastUpdateId.Value + 1) : client.GetUpdates();
                lastUpdateId = updates.LastOrDefault()?.UpdateId;
                ProcessUpdates(updates);
            } while (lastUpdateId != null);
        }

        private void Run()
        {
            while (true)
            {
                ProcessUpdates();
                Task.Delay(5000);
            }
        }

        private static BotConfig LoadConfig(string configPath)
        {
            using StreamReader reader = new(configPath, Encoding.UTF8);
            return new TextParser(TextParser.Settings.Default).Parse<BotConfig>(reader);
        }

        private static TelegramBotClient GetTelegramBotClient(IServiceProvider serviceProvider)
        {
            return new(serviceProvider.GetService<BotConfig>()!.ApiToken);
        }

        private static Tree GetTree(IServiceProvider serviceProvider)
        {
            return TreeSerializer.Load(serviceProvider.GetService<BotConfig>()!.TreeFile);
        }

        private static ResourceManager GetResourceManager(IServiceProvider serviceProvider) {
            return new ResourceManager("FiveLetters.Resources.Strings", typeof(BotApp).Assembly);
        }

        private static CultureInfo GetCultureInfo(IServiceProvider serviceProvider) {

            return new CultureInfo(serviceProvider.GetService<BotConfig>()!.Culture, false);
        }

        public static void Run(string[] args)
        {
            var services = new ServiceCollection();
            services.AddSingleton<BotApp>();
            services.AddSingleton(LoadConfig(args[0]));
            services.AddSingleton(GetTree);
            services.AddSingleton(GetTelegramBotClient);
            services.AddSingleton(GetResourceManager);
            services.AddSingleton(GetCultureInfo);

            ServiceProvider serviceProvider = services.BuildServiceProvider();
            serviceProvider.GetService<BotApp>()?.Run();
        }
    }
}