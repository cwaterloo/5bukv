namespace FiveLetters
{
    readonly struct Letter : IEquatable<Letter>
    {
        internal Letter(char value, Alphabet alphabet)
        {
            if (!alphabet.CharToIndex.ContainsKey(value))
            {
                throw new ArgumentException(string.Format(
                    "Provided alphabet has no character `{0}`.", value));
            }
            Value = alphabet.CharToIndex[value];
            _Alphabet = alphabet;
        }

        internal readonly char ToChar() => _Alphabet.IndexToChar[Value];

        public readonly bool Equals(Letter other) => Value == other.Value;

        public static bool operator ==(Letter left, Letter right) => left.Equals(right);

        public static bool operator !=(Letter left, Letter right) => !left.Equals(right);

        public static implicit operator int(Letter letter) => letter.Value;

        internal int Value { get; init; }

        private readonly Alphabet _Alphabet;

        public override readonly bool Equals(object? obj) => obj is Letter letter && Equals(letter);

        public override readonly int GetHashCode() => Value.GetHashCode();
    }
}