namespace FiveLetters
{
    public readonly struct Letter : IEquatable<Letter>
    {
        private readonly Alphabet _Alphabet;
        public int Value { get; init; }

        public Letter(char value, Alphabet alphabet)
        {
            if (!alphabet.CharToIndex.ContainsKey(value))
            {
                throw new ArgumentException(string.Format(
                    "Provided alphabet has no character `{0}`.", value));
            }
            Value = alphabet.CharToIndex[value];
            _Alphabet = alphabet;
        }

        public readonly char ToChar() => _Alphabet.IndexToChar[Value];

        public readonly bool Equals(Letter other) => Value == other.Value;

        public static bool operator ==(Letter left, Letter right) => left.Equals(right);

        public static bool operator !=(Letter left, Letter right) => !left.Equals(right);

        public static implicit operator int(Letter letter) => letter.Value;

        public override readonly bool Equals(object? obj) => obj is Letter letter && Equals(letter);

        public override readonly int GetHashCode() => Value.GetHashCode();
    }
}