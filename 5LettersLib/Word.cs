using System.Collections;
using System.Text;

namespace FiveLetters
{
    public struct Word : IEnumerable<Letter>, IEquatable<Word>
    {
        public const int WordLetterCount = 5;

        private readonly Letter[] _Letters;

        public readonly int AlphabetLetterCount => _Alphabet.IndexToChar.Count;

        public readonly Letter this[int index]
        {
            get => _Letters[index];
        }

        public override readonly string ToString()
        {
            StringBuilder stringBuilder = new(WordLetterCount);
            for (int i = 0; i < WordLetterCount; ++i)
            {
                stringBuilder.Append(_Letters[i].ToChar());
            }
            return stringBuilder.ToString();
        }

        public readonly IEnumerator<Letter> GetEnumerator() => _Letters.AsEnumerable().GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator() => _Letters.GetEnumerator();

        public readonly bool Equals(Word other)
        {
            for (int i = 0; i < WordLetterCount; ++i)
            {
                if (_Letters[i] != other._Letters[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static bool operator ==(Word left, Word right) => left.Equals(right);

        public static bool operator !=(Word left, Word right) => !left.Equals(right);

        private readonly Alphabet _Alphabet;

        public Word(string wordOrNull, Alphabet alphabet)
        {
            string word = wordOrNull ?? "";
            if (word.Length != WordLetterCount)
            {
                throw new ArgumentNullException(string.Format(
                    "Only {0} letter words are acceptable. The " +
                    "length of the word is {1}.", WordLetterCount, word.Length));
            }
            _Letters = new Letter[WordLetterCount];
            for (int i = 0; i < WordLetterCount; ++i)
            {
                _Letters[i] = new Letter(word[i], alphabet);
            }
            _Alphabet = alphabet;
        }

        public override readonly bool Equals(object? obj) => obj is Word word && Equals(word);

        public override readonly int GetHashCode() => _Letters
            .Select(letter => letter.GetHashCode()).Aggregate(0, (hashA, hashB) => hashA ^ hashB);
    }
}