using System.Text;

namespace FiveLetters
{
    public sealed class AlphabetUtils
    {
        public static List<char> GetAlphabet(params IReadOnlyList<string>[] wordLists)
        {
            HashSet<char> chars = [];
            foreach (IReadOnlyList<string> words in wordLists)
            {
                foreach (string word in words)
                {
                    foreach (char letter in word)
                    {
                        chars.Add(letter);
                    }
                }
            }

            return [.. chars.Order()];
        }

        public static Dictionary<char, int> GetReverseAlphabet(IReadOnlyList<char> alphabet)
        {
            Dictionary<char, int> result = [];

            for (int i = 0; i < alphabet.Count; ++i)
            {
                result[alphabet[i]] = i;
            }

            return result;
        }

        internal static string ToString(IReadOnlyList<char> alphabet)
        {
            StringBuilder stringBuilder = new();
            foreach (char chr in alphabet)
            {
                stringBuilder.Append(chr);
            }
            return stringBuilder.ToString();
        }

        private AlphabetUtils() { }
    }
}