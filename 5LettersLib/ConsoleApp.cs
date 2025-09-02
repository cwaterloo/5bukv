using System.IO.Compression;
using System.Text;

namespace FiveLetters
{
    public static class ConsoleApp
    {
        private record WordCollection
        {
            public List<string> AttackWords { get; init; } = [];
            public List<string> GlobalWords { get; init; } = [];
        };
        private readonly record struct LetterAndColors(char Letter, ConsoleColor ForegroundColor, ConsoleColor BackgroundColor);

        private static WordCollection LoadWords(IEnumerable<string> filenames)
        {
            HashSet<string> globalWords = [];
            HashSet<string>? attackWords = null;

            foreach (string filename in filenames)
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
                    Console.WriteLine("The dictionary `{0}` contains duplicates.", filename);
                }

                globalWords.UnionWith(words);

                if (attackWords == null)
                {
                    attackWords = words;
                }
                else
                {
                    attackWords.IntersectWith(words);
                }
            }

            return new WordCollection
            {
                AttackWords = [.. (attackWords ?? []).Order()],
                GlobalWords = [.. globalWords.Order()]
            };
        }

        private static int HiddenWordGame(string hiddenWord, ReadOnlyTree tree)
        {
            string guess = tree.Word;
            int attemptCount = 1;
            while (guess != hiddenWord)
            {
                tree = tree.Edges[new Evaluation(guess, hiddenWord).Pack()];
                guess = tree.Word;
                ++attemptCount;
            }
            return attemptCount;
        }

        private static void CollectStats(ReadOnlyTreeRoot root)
        {
            ReadOnlyTree tree = root.Tree;
            List<string> words = GetWords(tree);
            Console.WriteLine("Collecting stats...");
            int maxAttempts = 0;
            List<string> fails = [];
            Dictionary<int, int> attempts = [];
            for (int i = 0; i < words.Count; ++i)
            {
                int attempt_count = HiddenWordGame(words[i], tree);
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
            Console.WriteLine("Fail count: {0}, Max attempts: {1}.", fails.Count, maxAttempts);
            if (fails.Count > 0)
            {
                Console.WriteLine("Fail words: {0}.", string.Join(", ", fails));
            }
            foreach ((int attempt_count, int word_count) in attempts.OrderBy(pair => pair.Key))
            {
                Console.WriteLine("Attempt/word count: {0}/{1}.", attempt_count, word_count);
            }
        }

        private static LetterAndColors GetCharColor(char letter, char mask)
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

        private static void PrintEnteredState(string mask, string guess)
        {
            Console.Write("Entered state: ");
            foreach (LetterAndColors letterAndColors in guess.Zip(mask)
                .Select(letterCharTuple => GetCharColor(letterCharTuple.First, letterCharTuple.Second)))
            {
                Console.ForegroundColor = letterAndColors.ForegroundColor;
                Console.BackgroundColor = letterAndColors.BackgroundColor;
                Console.Write(letterAndColors.Letter);
            }
            Console.ResetColor();
            Console.WriteLine(".");
        }

        private static int? GetMask(string guess)
        {
            do
            {
                string matchPattern = new('y', guess.Length);
                Console.Write("Enter state (e.g gwwwh, g - not present, w - wrong place, " +
                    "y - correct place; {0} - to exit): ", matchPattern);
                string enteredValue = Console.ReadLine() ?? "";
                try
                {
                    Evaluation? state = enteredValue == matchPattern ? null : new Evaluation(guess, Evaluation.GetEvaluationTypes(enteredValue));
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

        private static void PlayInteractiveGame(ReadOnlyTreeRoot root)
        {
            ReadOnlyTree? localTree = root.Tree;
            int attempt = 0;
            do
            {
                string guess = localTree.Word;
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
                if (!localTree.Edges.TryGetValue(state.Value, out localTree) || localTree == null)
                {
                    Console.WriteLine("No words left. It means that one of the previous " +
                        "mask was entered incorrectly.");
                    break;
                }
            } while (attempt < 6);
        }

        private static void MakeGraph(string outputFilename, WordCollection words, bool dual)
        {
            TreeSerializer.Save(TreeGenerator.Get(words.GlobalWords, words.AttackWords, dual), outputFilename);
        }

        private static void MakeTuples(string outputFilename, WordCollection words)
        {
            using FileStream fileStream = new(outputFilename, FileMode.Create, FileAccess.Write, FileShare.None);
            using GZipStream gZipStream = new(fileStream, CompressionLevel.SmallestSize);
            using StreamWriter streamWriter = new(gZipStream, Encoding.UTF8);
            long count = TupleGenerator.Generate(words.AttackWords, 5, streamWriter);
            Console.WriteLine("Tuple count: {0}", count);
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
            Console.WriteLine("\t$ {0} graph true|false /path/to/nav_graph /path/to/dictionary ...", exeName);
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

        private static List<string> GetWords(ReadOnlyTree tree)
        {
            List<string> words = WordCollector.GetWords(tree);
            List<char> alphabet = AlphabetUtils.GetAlphabet(words);
            Console.WriteLine("Total unique characters in alphabet: {0}.", alphabet.Count);
            Console.WriteLine("Alphabet: {0}.", AlphabetUtils.ToString(alphabet));
            Console.WriteLine("Loaded {0} words.", words.Count);
            if (words.Count <= 0)
            {
                Console.WriteLine("The dictionary doesn't contain words.");
                Environment.Exit(1);
            }
            return words;
        }

        public static void Run(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            if (args.Length < 2 || args[0] == "graph" && args.Length < 4 || args[0] == "tuple" && args.Length < 3)
            {
                ShowHelpAndTerminate();
            }

            switch (args[0])
            {
                case "stats":
                    CollectStats(ReadOnlyTreeRoot.ValidateAndConvert(TreeSerializer.Load(args[1])));
                    break;
                case "interactive":
                    PlayInteractiveGame(ReadOnlyTreeRoot.ValidateAndConvert(TreeSerializer.Load(args[1])));
                    break;
                case "graph":
                    MakeGraph(args[2], LoadWords(args[3..]), bool.Parse(args[1]));
                    break;
                case "tuples":
                    MakeTuples(args[1], LoadWords(args[2..]));
                    break;
                default:
                    ShowHelpAndTerminate();
                    break;
            }
        }
    }
}