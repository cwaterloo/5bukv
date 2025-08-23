using System.Text;

namespace FiveLetters
{
    internal sealed class AlphabetUtils
    {
        internal static List<char> GetAlphabet(params IReadOnlyList<string>[] wordLists)
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