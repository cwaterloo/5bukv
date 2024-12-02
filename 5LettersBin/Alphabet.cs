using System.Text;

namespace FiveLetters
{
    sealed class Alphabet
    {
        internal IReadOnlyList<char> IndexToChar { get; init; }

        internal IReadOnlyDictionary<char, int> CharToIndex { get; init; }

        internal Alphabet(IReadOnlyList<char> IndexToChar)
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