namespace FiveLetters
{
    internal record LetterState {
        public int Min { get; set; } = 0;
        public int Max { get; set; } = Word.WordLetterCount;
    }

    internal record PositionState
        {
            internal required Letter Letter { get; init; }
            internal required bool Interpretation { get; init; }
            internal bool MatchLetter(Letter letter) => letter == Letter == Interpretation;
        }

    public class State
    {
        private readonly LetterState[] _LetterStates;

        private readonly PositionState[] _PositionStates;

        private static void Increment(Dictionary<Letter, int> counters, Letter letter)
        {
            if (counters.TryGetValue(letter, out int count))
            {
                counters[letter] = count + 1;
            }
            else
            {
                counters[letter] = 1;
            }
        }

        private static Dictionary<Letter, int> GetLetterCount(Word word)
        {
            Dictionary<Letter, int> wordLetterCounter = [];
            foreach (Letter letter in word)
            {
                Increment(wordLetterCounter, letter);
            }
            return wordLetterCounter;
        }

        private static LetterState[] CreateLetterStates(int length)
        {
            LetterState[] letterStates = new LetterState[length];
            for (int i = 0; i < letterStates.Length; ++i)
            {
                letterStates[i] = new();
            }

            return letterStates;
        }

        public State(Word word, Word guess)
        {
            if (guess.AlphabetLetterCount != word.AlphabetLetterCount)
            {
                throw new InvalidOperationException("An attempt to create a state from different alphabet length.");
            }

            _LetterStates = CreateLetterStates(word.AlphabetLetterCount);
            _PositionStates = new PositionState[Word.WordLetterCount];

            Dictionary<Letter, int> wordLetterCounter = GetLetterCount(word);
            Dictionary<Letter, int> guessLetterCounter = GetLetterCount(guess);

            foreach (KeyValuePair<Letter, int> guessLetterAndCount in guessLetterCounter)
            {
                int wordLetterCount = wordLetterCounter.GetValueOrDefault(guessLetterAndCount.Key, 0);
                if (wordLetterCount < guessLetterAndCount.Value)
                {
                    _LetterStates[guessLetterAndCount.Key].Min = wordLetterCount;
                    _LetterStates[guessLetterAndCount.Key].Max = wordLetterCount;
                }
                else
                {
                    _LetterStates[guessLetterAndCount.Key].Min = guessLetterAndCount.Value;
                }
            }

            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                _PositionStates[i] = new PositionState { Letter = guess[i], Interpretation = word[i] == guess[i] };
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
                if (_LetterStates[i].Min > metChars[i] || metChars[i] > _LetterStates[i].Max)
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

            _LetterStates = CreateLetterStates(guess.AlphabetLetterCount);
            _PositionStates = new PositionState[Word.WordLetterCount];

            Dictionary<Letter, int> letterCounter = [];
            HashSet<Letter> absentLetters = [];

            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                Letter letter = guess[i];
                switch (value[i])
                {
                    case 'g':
                        absentLetters.Add(letter);
                        _PositionStates[i] = new PositionState { Letter = letter, Interpretation = false };
                        break;
                    case 'w':
                        Increment(letterCounter, letter);
                        _PositionStates[i] = new PositionState { Letter = letter, Interpretation = false };
                        break;
                    case 'y':
                        Increment(letterCounter, letter);
                        _PositionStates[i] = new PositionState { Letter = letter, Interpretation = true };
                        break;
                    default:
                        throw new ArgumentException(string.Format(
                            "Value `{0}` contains at least one inacceptable " +
                            "character. Expecting only the following characters: " +
                            "`g`, `w`, `y`.", value));
                }
            }

            foreach (KeyValuePair<Letter, int> letterCount in letterCounter)
            {
                _LetterStates[letterCount.Key].Min = letterCount.Value;
                if (absentLetters.Contains(letterCount.Key))
                {
                    _LetterStates[letterCount.Key].Max = letterCount.Value;
                }
            }
        }
    }
}