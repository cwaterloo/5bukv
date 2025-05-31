using Microsoft.Extensions.DependencyInjection;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using FiveLetters.Data;
using System.Text;
using Telegram.BotAPI.UpdatingMessages;
using Telegram.BotAPI.Extensions;
using Google.Protobuf;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Builder;
using Telegram.BotAPI.GettingUpdates;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Globalization;

namespace FiveLetters
{
    internal record Msg(string Text, InlineKeyboardMarkup? Markup);

    internal record ChainStep(List<string> WordChain, List<Data.Evaluation> DefaultEvaluation, SessionStatus SessionStatus);

    internal enum SessionStatus
    {
        InProgress,
        Completed,
        Error
    }

    public sealed class BotApp(TelegramBotClient client, Tree tree, L10n l10n, CultureInfo cultureInfo, Config config, SortedDictionary<int, int> stat) : SimpleTelegramBotBase
    {
        public async Task<IResult> ProcessUpdateAsync(string secretToken, Update update, CancellationToken cancellationToken)
        {
            if (secretToken != config.SecretToken)
            {
                return Results.Unauthorized();
            }

            if (update == default)
            {
                return Results.BadRequest();
            }

            await OnUpdateAsync(update, cancellationToken);
            return Results.Ok();
        }


        protected override Task OnBotExceptionAsync(BotRequestException exp, CancellationToken cancellationToken = default)
        {
            if (exp.ErrorCode / 100 == 4)
            {
                return Task.CompletedTask;
            }

            throw new InvalidOperationException("Unknown bot request exception.", exp);
        }

        protected override Task OnExceptionAsync(Exception exp, CancellationToken cancellationToken = default)
        {
            if (exp is FormatException || exp is InvalidProtocolBufferException)
            {
                return Task.CompletedTask;
            }

            throw new InvalidOperationException("Unknown exception.", exp);
        }

        protected override async Task OnMessageAsync(Message message, CancellationToken cancellationToken = default)
        {
            switch (message.Text)
            {
                case "/start":
                    await ProcessStartAsync(message.Chat.Id, cancellationToken);
                    return;
                case "/help":
                    await ProcessHelpAsync(message.Chat.Id, cancellationToken);
                    return;
            }
        }

        protected async override Task OnCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken = default)
        {
            if (callbackQuery.Data == null || callbackQuery.Message == null)
            {
                return;
            }

            GameState gameState = GameStateSerializer.Load(callbackQuery.Data);
            if (gameState.Status == Status.ToBeDeleted)
            {
                await client.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                return;
            }

            Msg? msg = MakeMsg(gameState);
            if (msg != null)
            {
                await client.EditMessageTextAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId,
                    text: msg.Text, replyMarkup: msg.Markup, parseMode: "MarkdownV2", cancellationToken: cancellationToken);
            }
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
                SessionStatus.InProgress => l10n.GetResourceString("InProgress"),
                SessionStatus.Completed => l10n.GetResourceString("Completed"),
                SessionStatus.Error => l10n.GetResourceString("Error"),
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

        private ChainStep GetChainStep(IList<int> chain)
        {
            bool noWordsLeft = false;
            Tree lastTree = tree;
            List<string> wordChain = [];
            wordChain.Add(lastTree.Word);
            HashSet<char> presentLetters = [];
            Dictionary<int, char> correctLetters = [];
            foreach (int state in chain)
            {
                if (lastTree.Edges.TryGetValue(state, out Tree subtree))
                {
                    List<Data.Evaluation> evaluations = [.. Evaluation.Unpack(state).ToDataEvaluations()];
                    for (int i = 0; i < Word.WordLetterCount; ++i)
                    {
                        if (evaluations[i] != Data.Evaluation.Absent)
                        {
                            presentLetters.Add(lastTree.Word[i]);
                        }

                        if (evaluations[i] == Data.Evaluation.Correct)
                        {
                            correctLetters[i] = lastTree.Word[i];
                        }
                    }
                    lastTree = subtree;
                    wordChain.Add(lastTree.Word);
                }
                else
                {
                    noWordsLeft = true;
                    wordChain.Add(string.Format(cultureInfo, "\\[{0}\\]", l10n.GetResourceString("NoSuitableWords")));
                    break;
                }
            }

            List<Data.Evaluation> defaultEvaluations = [];
            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                if (correctLetters.TryGetValue(i, out char value) && value == lastTree.Word[i])
                {
                    defaultEvaluations.Add(Data.Evaluation.Correct);
                }
                else if (presentLetters.Contains(lastTree.Word[i]))
                {
                    defaultEvaluations.Add(Data.Evaluation.Present);
                }
                else
                {
                    defaultEvaluations.Add(Data.Evaluation.Absent);
                }
            }

            return new ChainStep(wordChain, defaultEvaluations, GetSessionStatus(lastTree.Edges.Count == 0, noWordsLeft));
        }

        private Msg? MakeMsg(GameState gameState)
        {
            if (gameState.Evaluation.Count != Word.WordLetterCount)
            {
                return null;
            }

            ChainStep chainStep = GetChainStep(gameState.Chain);
            string lastWord = chainStep.WordChain[^1];

            List<List<InlineKeyboardButton>> buttons = [];
            List<InlineKeyboardButton> buttonRowOne = [];
            List<InlineKeyboardButton> buttonRowTwo = [];

            StringBuilder textBuilder = new();
            switch (chainStep.SessionStatus)
            {
                case SessionStatus.Completed:
                    textBuilder.Append(string.Format(cultureInfo, l10n.GetResourceString("WordTemplate"), lastWord));
                    break;
                case SessionStatus.InProgress:
                    textBuilder.Append(string.Format(cultureInfo, l10n.GetResourceString("SuggestionTemplate"), lastWord));
                    break;
            }
            textBuilder.Append(string.Format(cultureInfo, l10n.GetResourceString("WordChainTemplate"),
                string.Join(" \\-\\> ", chainStep.WordChain)));
            textBuilder.Append(string.Format(cultureInfo, l10n.GetResourceString("StateTemplate"),
                GetSessionStatusText(chainStep.SessionStatus)));
            textBuilder.Append(string.Format(cultureInfo, l10n.GetResourceString("HelpTemplate"),
                "/help"));

            if (gameState.Chain.Count > 0 && gameState.Status == Status.Undefined)
            {
                // Back button
                GameState prevGameState = new()
                {
                    Chain = { gameState.Chain.SkipLast(1) },
                    Evaluation = { Evaluation.Unpack(gameState.Chain[^1]).ToDataEvaluations() }
                };

                buttonRowTwo.Add(new InlineKeyboardButton(l10n.GetResourceString("Back"))
                {
                    CallbackData = GameStateSerializer.Save(prevGameState)
                });
            }

            if (chainStep.SessionStatus == SessionStatus.InProgress)
            {
                // Forward button
                GameState nextGameState = new()
                {
                    Chain = { gameState.Chain.Append(Evaluation.FromDataEvaluations(gameState.Evaluation, lastWord).Pack()) },
                    Evaluation = { chainStep.DefaultEvaluation }
                };

                buttonRowTwo.Add(new InlineKeyboardButton(l10n.GetResourceString("Forward"))
                {
                    CallbackData = GameStateSerializer.Save(nextGameState)
                });

                // Letter buttons                
                GameState letterGameState = gameState.Clone();
                for (int i = 0; i < Word.WordLetterCount; ++i)
                {
                    letterGameState.Evaluation[i] = Next(letterGameState.Evaluation[i]);
                    buttonRowOne.Add(new InlineKeyboardButton(ToText(lastWord[i], gameState.Evaluation[i]))
                    { CallbackData = GameStateSerializer.Save(letterGameState) });
                    letterGameState.Evaluation[i] = Prev(letterGameState.Evaluation[i]);
                }
            }
            else if (gameState.Status == Status.Undefined)
            {
                GameState sealedGameState = gameState.Clone();
                sealedGameState.Status = Status.ToBeSealed;

                buttonRowTwo.Add(new InlineKeyboardButton(l10n.GetResourceString("Seal"))
                {
                    CallbackData = GameStateSerializer.Save(sealedGameState)
                });

                GameState deletedGameState = gameState.Clone();
                deletedGameState.Status = Status.ToBeDeleted;

                buttonRowTwo.Add(new InlineKeyboardButton(l10n.GetResourceString("Delete"))
                {
                    CallbackData = GameStateSerializer.Save(deletedGameState)
                });
            }

            if (buttonRowOne.Count > 0)
            {
                buttons.Add(buttonRowOne);
            }

            if (buttonRowTwo.Count > 0)
            {
                buttons.Add(buttonRowTwo);
            }

            return new Msg(textBuilder.ToString(), buttons.Count > 0 ? new InlineKeyboardMarkup(buttons) : null);
        }

        private async Task ProcessStartAsync(long chatId, CancellationToken cancellationToken)
        {
            Msg? msg = MakeMsg(MakeInitialGameState());
            if (msg != null)
            {
                await client.SendMessageAsync(chatId, text: msg.Text, replyMarkup: msg.Markup, parseMode: "MarkdownV2",
                    cancellationToken: cancellationToken);
            }
        }

        private async Task ProcessHelpAsync(long chatId, CancellationToken cancellationToken)
        {
            StringBuilder stringBuilder = new(l10n.GetResourceString("Help"));
            stringBuilder.Append('\n');
            string formatTemplate = l10n.GetResourceString("AttemptCountSlashWordsCount");
            stringBuilder.AppendJoin('\n', stat.Select(attemptCountToWordCount => string.Format(cultureInfo,
                formatTemplate, attemptCountToWordCount.Key, attemptCountToWordCount.Value)));
            stringBuilder.Append('\n');
            stringBuilder.AppendFormat(cultureInfo, l10n.GetResourceString("VocabularySize"), stat.Values.Sum());
            stringBuilder.Append('\n');
            stringBuilder.AppendFormat(cultureInfo, l10n.GetResourceString("Feedback"), config.FeedbackEmail!);
            await client.SendMessageAsync(chatId, text: stringBuilder.ToString(), parseMode: "MarkdownV2",
                cancellationToken: cancellationToken);
        }

        public static async Task RunAsync(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddSystemd();
            builder.Services.AddBotCommonServices();
            builder.Services.AddActivatedSingleton<BotApp>();
            builder.Host.UseSystemd();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.MapPost(app.Services.GetService<Config>()!.PathPattern!,
               async (CancellationToken cancellationToken, Update update,
                    [FromHeader(Name = "X-Telegram-Bot-Api-Secret-Token")] string secretToken, BotApp botApp) =>
                   await botApp.ProcessUpdateAsync(secretToken, update, cancellationToken))
                .WithName("Update")
                .WithOpenApi();

            await app.RunAsync();
        }
    }
}