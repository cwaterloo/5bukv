namespace FiveLetters
{
    internal record struct LetterState
    {
        public int Count { get; set; }
        public bool ExactMatch { get; set; }
    }

    internal record struct PositionState
        {
            internal int Letter { get; set; }
            internal bool Interpretation { get; set; }
            internal readonly bool MatchLetter(Letter letter) => letter == Letter == Interpretation;
        }

    public sealed class State
    {
        private readonly LetterState[] _LetterStates;

        private readonly PositionState[] _PositionStates;

        private static int[] GetLetterCount(Word word)
        {
            int[] wordLetterCounter = new int[word.AlphabetLetterCount];
            foreach (Letter letter in word)
            {
                ++wordLetterCounter[letter];
            }
            return wordLetterCounter;
        }

        public State(Word word, Word guess)
        {
            if (guess.AlphabetLetterCount != word.AlphabetLetterCount)
            {
                throw new InvalidOperationException("An attempt to create a state from different alphabet length.");
            }

            _LetterStates = new LetterState[word.AlphabetLetterCount];
            _PositionStates = new PositionState[Word.WordLetterCount];

            int[] wordLetterCounter = GetLetterCount(word);
            int[] guessLetterCounter = GetLetterCount(guess);

            for (int i = 0; i < guessLetterCounter.Length; ++i)
            {
                int wordLetterCount = wordLetterCounter[i];
                int guessLetterCount = guessLetterCounter[i];
                if (wordLetterCount < guessLetterCount)
                {
                    _LetterStates[i].Count = wordLetterCount;
                    _LetterStates[i].ExactMatch = true;
                }
                else
                {
                    _LetterStates[i].Count = guessLetterCount;
                }
            }

            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                _PositionStates[i].Letter = guess[i];
                _PositionStates[i].Interpretation = word[i] == guess[i];
            }
        }

        public bool MatchWord(Word word)
        {
            if (word.AlphabetLetterCount != _LetterStates.Length)
            {
                return false;
            }            
            int[] metChars = new int[_LetterStates.Length];
            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                if (!_PositionStates[i].MatchLetter(word[i]))
                {
                    return false;
                }
                ++metChars[word[i]];
            }

            for (int i = 0; i < _LetterStates.Length; ++i)
            {
                if (metChars[i] < _LetterStates[i].Count || _LetterStates[i].ExactMatch && metChars[i] != _LetterStates[i].Count)
                {
                    return false;
                }
            }
            return true;
        }

        public State(string value, Word guess)
        {
            if (value.Length != Word.WordLetterCount)
            {
                throw new ArgumentException(string.Format(
                    "The mask must contain exactly {0} characters.", Word.WordLetterCount));
            }

            _LetterStates = new LetterState[guess.AlphabetLetterCount];
            _PositionStates = new PositionState[Word.WordLetterCount];

            int[] letterCounter = new int[guess.AlphabetLetterCount];
            bool[] absentLetters = new bool[guess.AlphabetLetterCount];

            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                Letter letter = guess[i];
                switch (value[i])
                {
                    case 'g':
                        absentLetters[letter] = true;
                        _PositionStates[i] = new PositionState { Letter = letter, Interpretation = false };
                        break;
                    case 'w':
                        ++letterCounter[letter];
                        _PositionStates[i] = new PositionState { Letter = letter, Interpretation = false };
                        break;
                    case 'y':
                        ++letterCounter[letter];
                        _PositionStates[i] = new PositionState { Letter = letter, Interpretation = true };
                        break;
                    default:
                        throw new ArgumentException(string.Format(
                            "Value `{0}` contains at least one inacceptable " +
                            "character. Expecting only the following characters: " +
                            "`g`, `w`, `y`.", value));
                }
            }

            for (int i = 0; i < letterCounter.Length; ++i)
            {
                _LetterStates[i].Count = letterCounter[i];
                _LetterStates[i].ExactMatch = absentLetters[i];
            }
        }
    }
}