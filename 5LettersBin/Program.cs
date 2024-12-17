using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace FiveLetters
{
    internal class Program
    {
        internal readonly record struct LetterAndColors(Letter Letter, ConsoleColor ForegroundColor, ConsoleColor BackgroundColor);

        static Alphabet GetAlphabet(IReadOnlyList<string> words)
        {
            HashSet<char> chars = [];
            foreach (string word in words)
            {
                foreach (char letter in word)
                {
                    chars.Add(letter);
                }
            }

            return new(chars.Order().ToList().AsReadOnly());
        }

        static List<string> LoadWords(string filename)
        {
            HashSet<string> words = [];
            bool noDuplicates = true;
            using (StreamReader reader = new(filename, Encoding.UTF8))
            {
                string? word;
                while ((word = reader.ReadLine()) != null)
                {
                    noDuplicates &= words.Add(word);
                }
            }

            if (!noDuplicates)
            {
                Console.WriteLine("The dictionary contains duplicates.");
            }

            return [.. words.Order()];
        }

        static long GetMatchWordCount(List<Word> words, Word word, Word guess, long current, long observedMin)
        {
            long metric = current;
            long n = 0;
            IState state = MakeState(word, guess);
            foreach (Word wordToCheck in words)
            {
                if (state.MatchWord(wordToCheck))
                {
                    metric += (n << 1) + 1;
                    ++n;
                    if (metric > observedMin)
                    {
                        return metric;
                    }
                }
            }
            return metric;
        }

        static Word GetCandidate(List<Word> words, List<Word> globalWords)
        {
            if (words.Count == 1)
            {
                return words[0];
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            long minMetric = (long)words.Count * words.Count * words.Count * words.Count;
            Word? candidateMin = null;
            for (int i = 0; i < globalWords.Count; ++i)
            {
                long currentMetric = 0;
                foreach (Word word in words)
                {
                    currentMetric = GetMatchWordCount(words, word, globalWords[i], currentMetric, minMetric);
                    if (currentMetric > minMetric)
                    {
                        break;
                    }
                }
                if (currentMetric < minMetric)
                {
                    candidateMin = globalWords[i];
                    minMetric = currentMetric;
                }

                long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                if (elapsedMilliseconds > 60000)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "GetCandidate ETA: {0}",
                        DateTime.Now.AddMilliseconds(elapsedMilliseconds * (globalWords.Count - i - 1) / (i + 1))));
                }
            }

            stopwatch.Stop();

            if (candidateMin.HasValue)
            {
                return candidateMin.Value;
            }

            throw new InvalidOperationException("No more words left.");
        }

        static void GetMetric(List<Word> words, Word guess)
        {
            long metric = 0;
            foreach (Word word in words)
            {
                metric = GetMatchWordCount(words, word, guess,
                    metric, (long)words.Count * words.Count * words.Count * words.Count);
            }
            Console.WriteLine("Word: {0}, Metric {1}. Mean number of words: {2}.", guess,
                metric, Math.Round(metric / (double)words.Count));
        }

        static List<Word> FilterWords(List<Word> words, IState state)
        {
            List<Word> result = [];
            foreach (Word word in words)
            {
                if (state.MatchWord(word))
                {
                    result.Add(word);
                }
            }
            return result;
        }

        static int HiddenWordGame(int index, List<Word> globalWords, Word firstGuess)
        {
            List<Word> words = globalWords;
            Word hiddenWord = words[index];
            Word guess = firstGuess;
            int attemptCount = 1;
            while (guess != hiddenWord)
            {
                if (attemptCount == 6 && words.Count > 1)
                {
                    Console.Error.WriteLine("Number of words left at attempt 6: {0}", words.Count);
                }
                IState currentState = MakeState(hiddenWord, guess);
                words = FilterWords(words, currentState);
                guess = GetCandidate(words, globalWords);
                ++attemptCount;
            }
            return attemptCount;
        }

        static void GetFirstCandidate(List<Word> words)
        {
            Console.WriteLine("Getting first candidate...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            Word candidate = GetCandidate(words, words);
            stopwatch.Stop();
            Console.WriteLine("Candidate: {0}.", candidate);
            Console.WriteLine("Time Elapsed: {0}.", stopwatch.Elapsed);
        }

        static void CollectStats(List<Word> words, Word firstGuess)
        {
            Console.WriteLine("Collecting stats...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            int maxAttempts = 0;
            List<Word> fails = [];
            Dictionary<int, int> attempts = [];
            for (int i = 0; i < words.Count; ++i)
            {
                int attempt_count = HiddenWordGame(i, words, firstGuess);
                if (!attempts.TryAdd(attempt_count, 1))
                {
                    ++attempts[attempt_count];
                }
                if (attempt_count > 6)
                {
                    fails.Add(words[i]);
                }
                if (attempt_count > maxAttempts)
                {
                    maxAttempts = attempt_count;
                }
                long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                if (elapsedMilliseconds > 60000)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "CollectStats ETA: {0}",
                        DateTime.Now.AddMilliseconds(elapsedMilliseconds * (words.Count - i - 1) / (i + 1))));
                }
            }
            stopwatch.Stop();
            Console.WriteLine("Fail count: {0}, Max attempts: {1}.", fails.Count, maxAttempts);
            if (fails.Count > 0)
            {
                Console.WriteLine("Fail words: {0}.", string.Join(", ", fails));
            }
            foreach ((int attempt_count, int word_count) in attempts.OrderBy(pair => pair.Key))
            {
                Console.WriteLine("Attempt/word count: {0}/{1}.", attempt_count, word_count);
            }
            Console.WriteLine("Time Elapsed: {0}.", stopwatch.Elapsed);
        }

        static LetterAndColors GetCharColor(Letter letter, char mask)
        {
            switch (mask)
            {
                case 'g':
                    return new LetterAndColors(letter, ConsoleColor.White, ConsoleColor.DarkGray);
                case 'w':
                    return new LetterAndColors(letter, ConsoleColor.Black, ConsoleColor.White);
                case 'y':
                    return new LetterAndColors(letter, ConsoleColor.Black, ConsoleColor.Yellow);
                default:
                    // unreachable
                    return new LetterAndColors(letter, ConsoleColor.White, ConsoleColor.Red);
            }
        }

        static void PrintEnteredState(string mask, Word guess)
        {
            Console.Write("Entered state: ");
            foreach (LetterAndColors letterAndColors in guess.Zip(mask)
                .Select(letterCharTuple => GetCharColor(letterCharTuple.First, letterCharTuple.Second)))
            {
                Console.ForegroundColor = letterAndColors.ForegroundColor;
                Console.BackgroundColor = letterAndColors.BackgroundColor;
                Console.Write(letterAndColors.Letter.ToChar());
            }
            Console.ResetColor();
            Console.WriteLine(".");
        }

        static IState? GetMask(Word guess)
        {
            do
            {
                Console.Write("Enter state (e.g gwwwh, g - not present, w - wrong place, " +
                    "y - correct place; yyyyy - to exit): ");
                string enteredValue = Console.ReadLine() ?? "";
                try
                {
                    IState? state = enteredValue == "yyyyy" ? null : MakeState(enteredValue, guess);
                    PrintEnteredState(enteredValue, guess);
                    Console.WriteLine();
                    return state;
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Input `{0}` is incorrect, please repeat.", enteredValue);
                }
            } while (true);
        }

        static void PlayInteractiveGame(List<Word> globalWords, Word firstGuess)
        {
            Word guess = firstGuess;
            int attempt = 0;
            List<Word> words = globalWords;
            do
            {
                ++attempt;
                Console.WriteLine("There are {0} words left.", words.Count);
                if (words.Count < 10)
                {
                    Console.WriteLine("Words left: {0}.", string.Join(", ", words));
                }
                Console.Write("Attempt: {0}, Guess: ", attempt);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(guess);
                Console.ResetColor();
                Console.WriteLine(".");
                IState? state = GetMask(guess);
                if (state == null)
                {
                    break;
                }
                words = FilterWords(words, state);
                if (words.Count <= 0)
                {
                    Console.WriteLine("No words left. It means that one of the previous " +
                        "mask was entered incorrectly.");
                    break;
                }
                try
                {
                    guess = GetCandidate(words, globalWords);
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(string.Format("No candidates left. It means that one of the previous " +
                        "mask was entered incorrectly. Exception: {0}", ex));
                    break;
                }
            } while (attempt < 6);
        }

        private static IState MakeState(Word word, Word guess)
        {
            return new OldState(word, guess);
        }

        private static IState MakeState(string value, Word guess)
        {
            return new OldState(value, guess);
        }

        static string Contains(List<string> words, string word)
        {
            if (words.BinarySearch(word) < 0)
            {
                Console.WriteLine("The dictionary doesn't contain '{0}'", word);
                ShowHelpAndTerminate();
            }
            return word;
        }

        static void ShowHelpAndTerminate()
        {
            string exeName = AppDomain.CurrentDomain.FriendlyName;
            Console.WriteLine("Three way of usage: ");
            Console.WriteLine();
            Console.WriteLine("\t$ {0} first /path/to/dictionary", exeName);
            Console.WriteLine("\t\tComputes the best initial suggestion(s).");
            Console.WriteLine();
            Console.WriteLine("\t$ {0} stats /path/to/dictionary suggest", exeName);
            Console.WriteLine("\t\tCollects and shows stats.");
            Console.WriteLine();
            Console.WriteLine("\t$ {0} interactive /path/to/dictionary suggest", exeName);
            Console.WriteLine("\t\tStarts interactive mode to play the game.");
            Console.WriteLine();
            Console.WriteLine("\t$ {0} metric /path/to/dictionary suggest", exeName);
            Console.WriteLine("\t\tComputes the metric.");
            Console.WriteLine();
            Console.WriteLine("The '/path/to/dictionary' is path to a file that contains");
            Console.WriteLine("russian words.");
            Console.WriteLine("Each line of the file represents a single 5 letter russian word.");
            Console.WriteLine("All the letters must be in lowercase.");
            Console.WriteLine("The letter 'ё' must be replaced with 'е'.");
            Console.WriteLine("Duplicates are allowed but will be ignored.");
            Console.WriteLine("The dictionary must not be empty.");
            Console.WriteLine("The codepage must be UTF-8.");
            Console.WriteLine();
            Console.WriteLine("The 'suggest' is a 5 letter russian word that starts the game.");
            Console.WriteLine("The dictionary must contain the suggest.");
            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length < 2 || args[0] != "first" && args.Length < 3)
            {
                ShowHelpAndTerminate();
            }

            List<string> allWords = LoadWords(args[1]);
            Alphabet alphabet = GetAlphabet(allWords);
            Console.WriteLine("Total unique characters in alphabet: {0}.", alphabet.IndexToChar.Count);
            Console.WriteLine("Alphabet: {0}.", alphabet);
            List<Word> fiveLetterWords = allWords.Select(word => new Word(word, alphabet)).ToList();
            Console.WriteLine(string.Format("Loaded {0} words.", fiveLetterWords.Count));
            if (fiveLetterWords.Count <= 0)
            {
                Console.WriteLine("The dictionary doesn't contain words.");
                Environment.Exit(1);
            }

            switch (args[0])
            {
                case "first":
                    GetFirstCandidate(fiveLetterWords);
                    break;
                case "stats":
                    CollectStats(fiveLetterWords, new Word(Contains(allWords, args[2]), alphabet));
                    break;
                case "interactive":
                    PlayInteractiveGame(fiveLetterWords, new Word(Contains(allWords, args[2]), alphabet));
                    break;
                case "metric":
                    GetMetric(fiveLetterWords, new Word(Contains(allWords, args[2]), alphabet));
                    break;
                default:
                    ShowHelpAndTerminate();
                    break;
            }
        }
    }
}