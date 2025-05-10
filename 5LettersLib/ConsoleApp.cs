using System.Diagnostics;
using System.Text;
using FiveLetters.Data;

namespace FiveLetters
{
    public static class ConsoleApp
    {        
        private readonly record struct LetterAndColors(Letter Letter, ConsoleColor ForegroundColor, ConsoleColor BackgroundColor);

        private static List<string> LoadWords(string filename)
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

        private static int HiddenWordGame(Word hiddenWord, Tree tree, Alphabet alphabet)
        {
            Word guess = new(tree.Word, alphabet);
            int attemptCount = 1;
            while (guess != hiddenWord)
            {
                tree = tree.Edges[new Evaluation(hiddenWord, guess).Pack()];                
                guess = new(tree.Word, alphabet);
                ++attemptCount;
            }
            return attemptCount;
        }
        private static void CollectStats(Tree tree)
        {
            (List<Word> words, Alphabet alphabet) = GetFiveLetterWords(tree);
            Console.WriteLine("Collecting stats...");
            Stopwatch stopwatch = Stopwatch.StartNew();
            int maxAttempts = 0;
            List<Word> fails = [];
            Dictionary<int, int> attempts = [];            
            for (int i = 0; i < words.Count; ++i)
            {
                int attempt_count = HiddenWordGame(words[i], tree, alphabet);
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

        private static LetterAndColors GetCharColor(Letter letter, char mask)
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

        private static void PrintEnteredState(string mask, Word guess)
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

        private static int? GetMask(Word guess)
        {
            do
            {
                Console.Write("Enter state (e.g gwwwh, g - not present, w - wrong place, " +
                    "y - correct place; yyyyy - to exit): ");
                string enteredValue = Console.ReadLine() ?? "";
                try
                {
                    Evaluation? state = enteredValue == "yyyyy" ? null : new Evaluation(guess, enteredValue);
                    PrintEnteredState(enteredValue, guess);
                    Console.WriteLine();
                    return state?.Pack();
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Input `{0}` is incorrect, please repeat.", enteredValue);
                }
            } while (true);
        }

        private static void PlayInteractiveGame(Tree tree)
        {
            (_, Alphabet alphabet) = GetFiveLetterWords(tree);            
            int attempt = 0;
            do
            {
                Word guess = new(tree.Word, alphabet);
                ++attempt;
                Console.Write("Attempt: {0}, Guess: ", attempt);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(guess);
                Console.ResetColor();
                Console.WriteLine(".");
                int? state = GetMask(guess);
                if (state == null)
                {
                    break;
                }
                if (!tree.Edges.TryGetValue(state.Value, out tree))
                {
                    Console.WriteLine("No words left. It means that one of the previous " +
                        "mask was entered incorrectly.");
                    break;
                }
            } while (attempt < 6);
        }

        private static void MakeGraph(List<string> globalWords, string outputFilename)
        {
            (List<Word> allWords, _) = GetFiveLetterWords(globalWords);
            TreeSerializer.Save(TreeGenerator.Get(allWords), outputFilename);
        }

        private static void ShowHelpAndTerminate()
        {
            string exeName = AppDomain.CurrentDomain.FriendlyName;
            Console.WriteLine("Three way of usage: ");
            Console.WriteLine();
            Console.WriteLine("\t$ {0} stats /path/to/nav_graph", exeName);
            Console.WriteLine("\t\tCollects and shows stats. The navigation graph could");
            Console.WriteLine("\t\tbe obtained from a dictionary with the `graph` command (see below).");
            Console.WriteLine();
            Console.WriteLine("\t$ {0} interactive /path/to/nav_graph", exeName);
            Console.WriteLine("\t\tStarts interactive mode to play the game.");
            Console.WriteLine();
            Console.WriteLine("\t$ {0} graph /path/to/dictionary /path/to/nav_graph", exeName);
            Console.WriteLine("\t\tMakes the navigation graph out of the dictionary.");
            Console.WriteLine();
            Console.WriteLine("The '/path/to/dictionary' is path to a file that contains");
            Console.WriteLine("words.");
            Console.WriteLine("Each line of the file represents a single 5 letter russian word.");
            Console.WriteLine("All the letters must be in lowercase.");
            Console.WriteLine("Duplicates are allowed but will be ignored.");
            Console.WriteLine("The dictionary must not be empty.");
            Console.WriteLine("The codepage must be UTF-8.");
            Console.WriteLine();            
            Environment.Exit(1);
        }

        private static (List<Word> allWords, Alphabet alphabet) GetFiveLetterWords(List<string> allWords) {
            Alphabet alphabet = Alphabet.FromWords(allWords);
            Console.WriteLine("Total unique characters in alphabet: {0}.", alphabet.IndexToChar.Count);
            Console.WriteLine("Alphabet: {0}.", alphabet);
            List<Word> fiveLetterWords = allWords.Select(word => new Word(word, alphabet)).ToList();
            Console.WriteLine(string.Format("Loaded {0} words.", fiveLetterWords.Count));
            if (fiveLetterWords.Count <= 0)
            {
                Console.WriteLine("The dictionary doesn't contain words.");
                Environment.Exit(1);
            }
            return (fiveLetterWords, alphabet);
        }

        private static (List<Word> allWords, Alphabet alphabet) GetFiveLetterWords(Tree tree) {
            return GetFiveLetterWords(WordCollector.GetWords(tree));
        }

        public static void Run(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length < 2 || args[0] == "graph" && args.Length < 3)
            {
                ShowHelpAndTerminate();
            }

            switch (args[0])
            {
                case "stats":
                    CollectStats(TreeSerializer.Load(args[1]));
                    break;
                case "interactive":
                    PlayInteractiveGame(TreeSerializer.Load(args[1]));
                    break;
                case "graph":
                    MakeGraph(LoadWords(args[1]), args[2]);
                    break;
                default:
                    ShowHelpAndTerminate();
                    break;
            }
        }
    }
}