using System.Text;

namespace FiveLetters
{
    readonly record struct Position : IComparable<Position>
    {
        internal required Letter Letter { get; init; }
        internal required int Index { get; init; }
        public int CompareTo(Position other)
        {
            int result = Letter.Value.CompareTo(other.Letter.Value);
            return result == 0 ? Index.CompareTo(other.Index) : result;
        }

        public override string ToString()
        {
            return string.Concat(Index.ToString(), ' ', Letter.ToChar());
        }
    }

    readonly record struct CorrectnessState
    {
        internal required Position Position { get; init; }
        internal required bool Correct { get; init; }
    }

    record struct PresenceState : IComparable<PresenceState>
    {
        internal required Position Position { get; init; }
        internal required bool Present { get; set; }

        public readonly int CompareTo(PresenceState other) => Position.CompareTo(other.Position);
    }

    readonly struct State
    {

        private readonly PresenceState[] _PresenceStates;

        private readonly CorrectnessState[] _CorrectnessStates;

        internal State(Word word, Word guess)
        {
            _CorrectnessStates = new CorrectnessState[Word.WordLetterCount];
            _PresenceStates = new PresenceState[Word.WordLetterCount];
            Position[] positions = new Position[Word.WordLetterCount];
            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                _CorrectnessStates[i] = new CorrectnessState
                {
                    Position = new Position { Letter = guess[i], Index = i },
                    Correct = guess[i] == word[i]
                };
                _PresenceStates[i] = new PresenceState { Position = _CorrectnessStates[i].Position, Present = false };
                positions[i] = new Position { Letter = word[i], Index = i };
            }

            Array.Sort(_PresenceStates);
            Array.Sort(positions);

            int wordCarriage = 0; // positions
            int guessCarriage = 0; // _PresenceStates

            while (wordCarriage < positions.Length && guessCarriage < _PresenceStates.Length)
            {
                if (_CorrectnessStates[_PresenceStates[guessCarriage].Position.Index].Correct)
                {
                    ++guessCarriage;
                    continue;
                }

                if (_CorrectnessStates[positions[wordCarriage].Index].Correct)
                {
                    ++wordCarriage;
                    continue;
                }

                if (_PresenceStates[guessCarriage].Position.Letter.Value < positions[wordCarriage].Letter.Value)
                {
                    ++guessCarriage;
                    continue;
                }

                if (positions[wordCarriage].Letter.Value < _PresenceStates[guessCarriage].Position.Letter.Value)
                {
                    ++wordCarriage;
                    continue;
                }

                _PresenceStates[guessCarriage].Present = true;
                ++guessCarriage;
                ++wordCarriage;
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new();
            for (int i = 0; i < _CorrectnessStates.Length; ++i)
            {
                stringBuilder.Append(_CorrectnessStates[i].Correct ? 'C' : 'I');
            }
            stringBuilder.Append(' ');
            for (int i = 0; i < _PresenceStates.Length; ++i)
            {
                stringBuilder.Append(_PresenceStates[i].Position.Index);
            }
            stringBuilder.Append(' ');
            for (int i = 0; i < _PresenceStates.Length; ++i)
            {
                stringBuilder.Append(_PresenceStates[i].Position.Letter.ToChar());
            }
            stringBuilder.Append(' ');
            for (int i = 0; i < _PresenceStates.Length; ++i)
            {
                stringBuilder.Append(_PresenceStates[i].Present ? 'P' : 'A');
            }

            return stringBuilder.ToString();
        }

        internal readonly bool MatchWord(Word word)
        {
            Position[] positions = new Position[Word.WordLetterCount];
            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                if (_CorrectnessStates[i].Correct == (word[i] != _CorrectnessStates[i].Position.Letter))
                {
                    return false;
                }
                positions[i] = new Position { Letter = word[i], Index = i };
            }

            Array.Sort(positions);

            int wordCarriage = 0; // positions
            int stateCarriage = 0; // _PresenceStates

            while (wordCarriage < positions.Length && stateCarriage < _PresenceStates.Length)
            {
                if (_CorrectnessStates[_PresenceStates[stateCarriage].Position.Index].Correct)
                {
                    ++stateCarriage;
                    continue;
                }

                if (_CorrectnessStates[positions[wordCarriage].Index].Correct)
                {
                    ++wordCarriage;
                    continue;
                }

                if (positions[wordCarriage].Letter.Value < _PresenceStates[stateCarriage].Position.Letter.Value)
                {
                    ++wordCarriage;
                    continue;
                }

                if (_PresenceStates[stateCarriage].Position.Letter.Value < positions[wordCarriage].Letter.Value)
                {
                    if (_PresenceStates[stateCarriage].Present)
                    {
                        return false;
                    }
                    ++stateCarriage;
                    continue;
                }

                if (!_PresenceStates[stateCarriage].Present)
                {
                    return false;
                }
                ++wordCarriage;
                ++stateCarriage;
            }

            while (stateCarriage < _PresenceStates.Length)
            {
                if (_PresenceStates[stateCarriage].Present)
                {
                    return false;
                }
                ++stateCarriage;
            }

            return true;
        }

        internal State(string value, Word guess)
        {
            if (value.Length != Word.WordLetterCount)
            {
                throw new ArgumentException(string.Format(
                    "The mask must contain exactly {0} characters.", Word.WordLetterCount));
            }

            _CorrectnessStates = new CorrectnessState[Word.WordLetterCount];
            _PresenceStates = new PresenceState[Word.WordLetterCount];

            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                Letter letter = guess[i];
                switch (value[i])
                {
                    case 'g':
                        _CorrectnessStates[i] = new CorrectnessState { Position = new() { Index = i, Letter = letter }, Correct = false };
                        _PresenceStates[i] = new PresenceState { Position = _CorrectnessStates[i].Position, Present = false };
                        break;
                    case 'w':
                        _CorrectnessStates[i] = new CorrectnessState { Position = new() { Index = i, Letter = letter }, Correct = false };
                        _PresenceStates[i] = new PresenceState { Position = _CorrectnessStates[i].Position, Present = true };
                        break;
                    case 'y':
                        _CorrectnessStates[i] = new CorrectnessState { Position = new() { Index = i, Letter = letter }, Correct = true };
                        _PresenceStates[i] = new PresenceState { Position = _CorrectnessStates[i].Position, Present = false };
                        break;
                    default:
                        throw new ArgumentException(string.Format(
                            "Value `{0}` contains at least one inacceptable " +
                            "character. Expecting only the following characters: " +
                            "`g`, `w`, `y`.", value));
                }
            }

            Array.Sort(_PresenceStates);
        }
    }
}