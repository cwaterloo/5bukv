using System.Text;

namespace FiveLetters
{
    public sealed class Alphabet
    {
        public IReadOnlyList<char> IndexToChar { get; init; }

        public IReadOnlyDictionary<char, int> CharToIndex { get; init; }

        public static Alphabet FromWords(IReadOnlyList<string> words)
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

        public Alphabet(IReadOnlyList<char> IndexToChar)
        {
            this.IndexToChar = IndexToChar;
            Dictionary<char, int> dict = [];
            for (int i = 0; i < IndexToChar.Count; ++i)
            {
                dict[IndexToChar[i]] = i;
            }
            CharToIndex = dict.AsReadOnly();
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            foreach (char chr in IndexToChar)
            {
                stringBuilder.Append(chr);
            }
            return stringBuilder.ToString();
        }
    }
}