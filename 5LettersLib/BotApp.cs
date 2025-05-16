using Microsoft.Extensions.DependencyInjection;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;
using FiveLetters.Data;
using System.Text;
using Telegram.BotAPI.UpdatingMessages;
using System.Resources;
using System.Globalization;
using Google.Protobuf;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FiveLetters
{
    internal record Msg(string Text, InlineKeyboardMarkup Markup);

    internal enum SessionStatus
    {
        InProgress,
        Completed,
        Error
    }

    public sealed class AppSettings(IConfiguration configuration)
    {
        public string ApiToken => configuration.GetSection("AppSettings:ApiToken").Value!;
        public string Culture => configuration.GetSection("AppSettings:Culture").Value!;
        public string TreeFile => configuration.GetSection("AppSettings:TreeFile").Value!;
        public string SecretToken => configuration.GetSection("AppSettings:SecretToken").Value!;
    }

    public sealed class SecretToken(string token)
    {
        public string Value => token;
    }

    public sealed class BotApp(TelegramBotClient client, Tree tree, CultureInfo cultureInfo, ResourceManager resourceManager, SecretToken token)
    {
        private void ProcessUpdate(Update update, string secretToken)
        {
            if (token.Value != secretToken)
            {
                return;
            }

            if (update.Message != null)
            {
                ProcessMessage(update.Message);
            }
            else if (update.CallbackQuery != null)
            {
                ProcessCallbackQuery(update.CallbackQuery);
            }
        }

        private string GetResourceString(string key)
        {
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

        private string ToText(char letter, Data.Evaluation evaluation)
        {
            return string.Format(cultureInfo, "{0}{1}", GetEvaluationChar(evaluation), letter);
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

            GameState gameState;

            try
            {
                gameState = GameStateSerializer.Load(callbackQuery.Data);
            }
            catch (Exception ex) when (ex is FormatException || ex is InvalidProtocolBufferException)
            {
                return;
            }

            Msg? msg = MakeMsg(gameState);
            if (msg != null)
            {
                try
                {
                    client.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId,
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

        private static TelegramBotClient GetTelegramBotClient(IServiceProvider serviceProvider)
        {
            return new(serviceProvider.GetService<AppSettings>()!.ApiToken);
        }

        private static Tree GetTree(IServiceProvider serviceProvider)
        {
            return TreeSerializer.Load(serviceProvider.GetService<AppSettings>()!.TreeFile);
        }

        private static CultureInfo GetCultureInfo(IServiceProvider serviceProvider)
        {
            return new CultureInfo(serviceProvider.GetService<AppSettings>()!.Culture, false);
        }

        private static SecretToken GetSecretToken(IServiceProvider serviceProvider)
        {
            return new SecretToken(serviceProvider.GetService<AppSettings>()!.SecretToken);
        }

        public static void Run(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSingleton(GetTree);
            builder.Services.AddSingleton(GetTelegramBotClient);
            builder.Services.AddSingleton(new ResourceManager("FiveLetters.Resources.Strings", typeof(BotApp).Assembly));
            builder.Services.AddSingleton(GetCultureInfo);
            builder.Services.AddSingleton(GetSecretToken);
            builder.Services.AddSingleton<AppSettings>();
            builder.Services.AddSingleton<BotApp>();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapPost("/update",
                (Update update, [FromHeader(Name = "X-Telegram-Bot-Api-Secret-Token")] string secretToken, BotApp botApp) =>
                    botApp.ProcessUpdate(update, secretToken))
                .WithName("Update")
                .WithOpenApi();

            app.Run();
        }
    }
}