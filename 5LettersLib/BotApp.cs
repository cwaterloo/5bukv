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
using System.Collections.Immutable;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace FiveLetters
{
    internal record Msg(string Text, InlineKeyboardMarkup? Markup);

    internal record LettersInfo(IImmutableDictionary<char, int> LetterCounts, IImmutableDictionary<int, char> CorrectLetters);

    internal record NextInfo(string? Word, int PackedEvaluations);

    internal record ChainStep(IImmutableList<string> WordChain, LettersInfo LettersInfo, SessionStatus SessionStatus, NextInfo NextInfo);

    internal enum SessionStatus
    {
        InProgress,
        Completed,
        Error
    }

    public sealed class BotApp(TelegramBotClient client, ReadOnlyTreeRoot root, L10n l10n, CultureInfo cultureInfo,
        Config config, ImmutableSortedDictionary<int, int> stat, MemoizedValue<string> helpString,
        ILogger<BotApp> logger) : SimpleTelegramBotBase
    {
        private async Task<IResult> ProcessUpdateAsync(string secretToken, Update update, CancellationToken cancellationToken)
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

            ExceptionDispatchInfo.Capture(exp).Throw();
            throw exp; // Unreachable
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
                Log("Delete.", callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
                await client.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId, cancellationToken);
                return;
            }

            Msg? msg = MakeMsg(gameState, callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
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

        private IEnumerable<Data.Evaluation> GetDefaultEvaluation(string? word, LettersInfo lettersInfo)
        {
            if (word == null)
            {
                return GetDefaultEvaluation();
            }

            List<Data.Evaluation> list = [];
            for (int i = 0; i < word.Length; ++i)
            {
                list.Add(Data.Evaluation.Absent);
            }

            Dictionary<char, int> letterCounts = lettersInfo.LetterCounts.ToDictionary();
            for (int i = 0; i < word.Length; ++i)
            {
                int count = letterCounts.GetValueOrDefault(word[i], 0);
                if (count <= 0)
                {
                    continue;
                }

                if (lettersInfo.CorrectLetters.TryGetValue(i, out char value) && value == word[i])
                {
                    letterCounts[word[i]] = count - 1;
                    list[i] = Data.Evaluation.Correct;
                }
            }

            for (int i = 0; i < word.Length; ++i)
            {
                if (list[i] != Data.Evaluation.Absent)
                {
                    continue;
                }

                int count = letterCounts.GetValueOrDefault(word[i], 0);
                if (count > 0)
                {
                    letterCounts[word[i]] = count - 1;
                    list[i] = Data.Evaluation.Present;
                }
            }

            return list;
        }

        private IEnumerable<Data.Evaluation> GetDefaultEvaluation()
        {
            for (int i = 0; i < root.WordLength; ++i)
            {
                yield return Data.Evaluation.Absent;
            }
        }

        private GameState MakeInitialGameState()
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

        private static void UpdatePresence(IReadOnlyList<Data.Evaluation> evaluations, string word,
            Dictionary<char, int> letterCounts, Dictionary<int, char> correctLetters)
        {
            Dictionary<char, int> newLetterCounts = [];
            for (int i = 0; i < word.Length; ++i)
            {
                switch (evaluations[i])
                {
                    case Data.Evaluation.Correct:
                        correctLetters[i] = word[i];
                        goto case Data.Evaluation.Present;
                    case Data.Evaluation.Present:
                        newLetterCounts[word[i]] = newLetterCounts.GetValueOrDefault(word[i], 0) + 1;
                        break;
                }
            }

            foreach ((char letter, int count) in newLetterCounts)
            {
                if (letterCounts.TryGetValue(letter, out int originalCount))
                {
                    letterCounts[letter] = Math.Max(count, originalCount);
                }
                else
                {
                    letterCounts[letter] = count;
                }
            }
        }

        private static char ToChar(Data.Evaluation evaluation)
        {
            return evaluation switch
            {
                Data.Evaluation.Absent => 'g',
                Data.Evaluation.Correct => 'y',
                Data.Evaluation.Present => 'w',
                _ => throw new InvalidDataException(string.Format("Unknown enum value: {0}", evaluation)),
            };
        }

        private static string ToString(IReadOnlyList<Data.Evaluation> evaluations)
        {
            StringBuilder builder = new();
            foreach (Data.Evaluation evaluation in evaluations)
            {
                builder.Append(ToChar(evaluation));
            }
            return builder.ToString();
        }

        private ChainStep GetChainStep(IReadOnlyList<int> chain, IReadOnlyList<Data.Evaluation> currentWordEvaluations)
        {
            bool noWordsLeft = false;
            ReadOnlyTree lastTree = root.Tree;
            List<string> wordChain = [];
            wordChain.Add(lastTree.Word);
            Dictionary<char, int> letterCounts = [];
            Dictionary<int, char> correctLetters = [];
            foreach (int state in chain)
            {
                if (lastTree.Edges.TryGetValue(state, out ReadOnlyTree? subtree) && subtree != null)
                {
                    UpdatePresence([.. Evaluation.Unpack(state, lastTree.Word).ToDataEvaluations()], lastTree.Word, letterCounts, correctLetters);
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

            int packedEvaluations = Evaluation.FromDataEvaluations(currentWordEvaluations, lastTree.Word).Pack();
            string? word = lastTree.Edges.GetValueOrDefault(packedEvaluations)?.Word;
            UpdatePresence(currentWordEvaluations, lastTree.Word, letterCounts, correctLetters);
            return new ChainStep(wordChain.ToImmutableList(), new LettersInfo(letterCounts.ToImmutableDictionary(),
                correctLetters.ToImmutableDictionary()),
                GetSessionStatus(lastTree.Edges.Count == 0, noWordsLeft), new NextInfo(word, packedEvaluations));
        }

        private void LogState(IImmutableList<string> wordChain, IReadOnlyList<Data.Evaluation> evaluations, SessionStatus sessionStatus,
            bool isSealed, long chatId, int? messageId)
        {
            string chain = string.Join(" -> ", wordChain);
            if (sessionStatus == SessionStatus.InProgress)
            {
                Log(string.Format(CultureInfo.InvariantCulture,
                    "Move: {0}, Evaluations: {1}.", chain, ToString(evaluations)), chatId, messageId);
            }
            else
            {
                Log(string.Format(CultureInfo.InvariantCulture,
                    "Move: {0}, Status: {1}, Sealed: {2}.", chain, sessionStatus, isSealed), chatId, messageId);
            }
        }

        private Msg? MakeMsg(GameState gameState, long chatId, int? messageId = null)
        {
            if (gameState.Evaluation.Count != root.WordLength)
            {
                return null;
            }

            ChainStep chainStep = GetChainStep(gameState.Chain, gameState.Evaluation);
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
            LogState(chainStep.WordChain, gameState.Evaluation, chainStep.SessionStatus,
                gameState.Status == Status.ToBeSealed, chatId, messageId);
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
                    Evaluation = { Evaluation.Unpack(gameState.Chain[^1], chainStep.WordChain[^2]).ToDataEvaluations() }
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
                    Chain = { gameState.Chain.Append(chainStep.NextInfo.PackedEvaluations) },
                    Evaluation = { GetDefaultEvaluation(chainStep.NextInfo.Word, chainStep.LettersInfo) }
                };

                buttonRowTwo.Add(new InlineKeyboardButton(l10n.GetResourceString("Forward"))
                {
                    CallbackData = GameStateSerializer.Save(nextGameState)
                });

                // Letter buttons                
                GameState letterGameState = gameState.Clone();
                for (int i = 0; i < letterGameState.Evaluation.Count; ++i)
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
            Msg? msg = MakeMsg(MakeInitialGameState(), chatId);
            if (msg != null)
            {
                await client.SendMessageAsync(chatId, text: msg.Text, replyMarkup: msg.Markup, parseMode: "MarkdownV2",
                    cancellationToken: cancellationToken);
            }
        }

        private void Log(string message, long chatId, int? messageId = null)
        {
            string chatIdHash = Convert.ToBase64String(SHA3_256.HashData(BitConverter.GetBytes(chatId)));
            if (messageId.HasValue)
            {
                string messageIdHash = Convert.ToBase64String(SHA3_256.HashData(BitConverter.GetBytes(messageId.Value)));
                logger.LogDebug("{chatIdHash}_{messageIdHash}: {message}", chatIdHash, messageIdHash, message);
            }
            else
            {
                logger.LogDebug("{chatIdHash}: {message}", chatIdHash, message);
            }
        }

        private async Task ProcessHelpAsync(long chatId, CancellationToken cancellationToken)
        {
            Log("Help.", chatId);
            StringBuilder stringBuilder = new(helpString.Get());
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