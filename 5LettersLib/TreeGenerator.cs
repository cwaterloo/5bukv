using System.Text;
using FiveLetters.Data;

namespace FiveLetters
{
    internal sealed class TreeGenerator
    {
        private readonly IReadOnlyList<Word> attackWords;
        private readonly IReadOnlyList<char> alphabet;

        private TreeGenerator(IReadOnlyList<Word> attackWords, IReadOnlyList<char> alphabet)
        {
            this.attackWords = attackWords;
            this.alphabet = alphabet;
        }

        internal static Tree Get(IReadOnlyList<string> globalWordStrings, IReadOnlyList<string> attackWordStrings, bool dual)
        {
            (List<Word> globalWords, List<Word> attackWords, List<char> alphabet) = GetWords(globalWordStrings, attackWordStrings);
            TreeGenerator generator = new(attackWords, alphabet);
            return dual ? generator.Make2(globalWords, 0) : generator.Make(globalWords, 0);
        }

        private string ToString(Word word)
        {
            StringBuilder sb = new();
            foreach (int letter in word)
            {
                sb.Append(alphabet[letter]);
            }

            return sb.ToString();
        }

        private static (List<Word> globalWords, List<Word> attackWords, List<char> alphabet) GetWords(
            IReadOnlyList<string> globalWordStrings, IReadOnlyList<string> attackWordStrings)
        {
            List<char> alphabet = AlphabetUtils.GetAlphabet(globalWordStrings, attackWordStrings);
            Dictionary<char, int> charToLetterMap = GetReverseAlphabet(alphabet);
            Console.WriteLine("Total unique characters in alphabet: {0}.", alphabet.Count);
            Console.WriteLine("Alphabet: {0}.", AlphabetUtils.ToString(alphabet));
            List<Word> globalWords = [.. globalWordStrings.Select(word => ToWord(word, charToLetterMap))];
            List<Word> attackWords = [.. attackWordStrings.Select(word => ToWord(word, charToLetterMap))];
            Console.WriteLine("Loaded {0} global words and {1} attack words.", globalWords.Count, attackWords.Count);
            if (globalWords.Count <= 0)
            {
                Console.WriteLine("The global dictionary doesn't contain words.");
                Environment.Exit(1);
            }
            if (attackWords.Count <= 0)
            {
                Console.WriteLine("The attack dictionary doesn't contain words.");
                Environment.Exit(1);
            }
            return (globalWords, attackWords, alphabet);
        }

        private static Word ToWord(string word, IReadOnlyDictionary<char, int> map)
        {
            List<int> result = [];
            foreach (char c in word)
            {
                result.Add(map[c]);
            }

            return result.AsReadOnly();
        }

        private static Dictionary<char, int> GetReverseAlphabet(IReadOnlyList<char> alphabet)
        {
            Dictionary<char, int> result = [];

            for (int i = 0; i < alphabet.Count; ++i)
            {
                result[alphabet[i]] = i;
            }

            return result;
        }

        private Tree Make(IReadOnlyList<Word> candidates, int level)
        {
            Word guess = AI.GetCandidate(candidates, attackWords, alphabet.Count);
            Dictionary<int, List<Word>> stateWords = [];
            foreach (Word hiddenWord in candidates)
            {
                int packedState = new Evaluation(ToString(guess), ToString(hiddenWord)).Pack();
                List<Word> words = stateWords.TryGetValue(packedState, out List<Word>? value) ?
                    value : stateWords[packedState] = [];
                words.Add(hiddenWord);
            }

            Dictionary<int, Tree> edges = stateWords.Count == 1 ? [] :
                stateWords.ToDictionary(keyValue => keyValue.Key, keyValue => Make(keyValue.Value, level + 1));
            return new()
            {
                Word = ToString(guess),
                Edges = { edges }
            };
        }

        private Tree Make2(IReadOnlyList<Word> candidates, int level)
        {
            (Word firstGuess, Word secondGuess) = AI.GetCandidate2(candidates, attackWords, alphabet.Count);
            Dictionary<int, Dictionary<int, List<Word>>> stateWords = [];
            foreach (Word hiddenWord in candidates)
            {
                int firstPackedState = new Evaluation(ToString(firstGuess), ToString(hiddenWord)).Pack();
                Dictionary<int, List<Word>> firstWords = stateWords.TryGetValue(firstPackedState,
                    out Dictionary<int, List<Word>>? value1) ? value1 : stateWords[firstPackedState] = [];
                int secondPackedState = new Evaluation(ToString(secondGuess), ToString(hiddenWord)).Pack();
                List<Word> secondWords = firstWords.TryGetValue(secondPackedState, out List<Word>? value2) ?
                     value2 : firstWords[secondPackedState] = [];
                secondWords.Add(hiddenWord);
            }

            string secondGuessString = ToString(secondGuess);
            Dictionary<int, Tree> edges = [];
            foreach (KeyValuePair<int, Dictionary<int, List<Word>>> firstKeyValue in stateWords)
            {
                Dictionary<int, List<Word>> firstValue = firstKeyValue.Value;
                if (firstValue.Values.Count == 1)
                {
                    List<Word> words = firstValue.Values.Single();
                    if (words.Count == 1)
                    {
                        edges.Add(firstKeyValue.Key, Make(words, level + 1));
                        continue;
                    }
                }

                edges.Add(firstKeyValue.Key, new Tree { Word = secondGuessString, Edges = {
                    firstKeyValue.Value.ToDictionary(secondKeyValue => secondKeyValue.Key,
                        secondKeyValue => Make(secondKeyValue.Value, level + 2)) } });
            }

            return new()
            {
                Word = ToString(firstGuess),
                Edges = { edges }
            };
        }
    }
}
