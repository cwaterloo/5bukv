global using Word = System.Collections.Generic.IReadOnlyList<int>;

namespace FiveLetters
{
    public static class AI
    {
        private record struct LetterState
        {
            internal int Count { get; set; }
            internal bool ExactMatch { get; set; }

            internal readonly bool MatchCount(int count)
            {
                if (ExactMatch)
                {
                    return count == Count;
                }

                return count >= Count;
            }
        }

        private record struct PositionState
        {
            internal int Letter { get; set; }
            internal bool Interpretation { get; set; }
            internal readonly bool MatchLetter(int letter) => letter == Letter == Interpretation;
        }

        private sealed class State(int length, int alphabetPower)
        {
            private readonly LetterState[] letterStates = new LetterState[alphabetPower];

            private readonly PositionState[] positionStates = new PositionState[length];

            private readonly int[] hiddenLetterCount = new int[alphabetPower];

            private readonly int[] guessLetterCount = new int[alphabetPower];

            private readonly int[] metChars = new int[alphabetPower];

            private static void GetLetterCount(Word word, int[] counter)
            {
                Array.Clear(counter);
                foreach (int letter in word)
                {
                    ++counter[letter];
                }
            }

            public void Init(Word word, Word guess)
            {
                for (int i = 0; i < positionStates.Length; ++i)
                {
                    positionStates[i].Letter = guess[i];
                    positionStates[i].Interpretation = word[i] == guess[i];
                }

                GetLetterCount(word, hiddenLetterCount);
                GetLetterCount(guess, guessLetterCount);

                for (int i = 0; i < guessLetterCount.Length; ++i)
                {
                    int wordLetterCount = hiddenLetterCount[i];
                    int guessLetterCount = this.guessLetterCount[i];
                    if (wordLetterCount < guessLetterCount)
                    {
                        letterStates[i].Count = wordLetterCount;
                        letterStates[i].ExactMatch = true;
                    }
                    else
                    {
                        letterStates[i].Count = guessLetterCount;
                        letterStates[i].ExactMatch = false;
                    }
                }
            }

            public bool MatchWord(Word word)
            {
                Array.Clear(metChars);
                for (int i = 0; i < word.Count; ++i)
                {
                    if (!positionStates[i].MatchLetter(word[i]))
                    {
                        return false;
                    }
                    ++metChars[word[i]];
                }

                for (int i = 0; i < letterStates.Length; ++i)
                {
                    if (!letterStates[i].MatchCount(metChars[i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private static long GetMatchWordCount(State firstState, State secondState, IReadOnlyList<Word> words, Word word,
            Word firstGuess, Word secondGuess, long current, long observedMin)
        {
            long metric = current;
            long n = 0;
            firstState.Init(word, firstGuess);
            secondState.Init(word, secondGuess);
            foreach (Word wordToCheck in words)
            {
                if (firstState.MatchWord(wordToCheck) && secondState.MatchWord(wordToCheck))
                {
                    metric += (n << 1) + 1;
                    ++n;
                    if (metric > observedMin)
                    {
                        return metric;
                    }
                }
            }
            return metric;
        }

        private static long GetMatchWordCount(State state, IReadOnlyList<Word> words, Word word, Word guess,
            long current, long observedMin)
        {
            long metric = current;
            long n = 0;
            state.Init(word, guess);
            foreach (Word wordToCheck in words)
            {
                if (state.MatchWord(wordToCheck))
                {
                    metric += (n << 1) + 1;
                    ++n;
                    if (metric > observedMin)
                    {
                        return metric;
                    }
                }
            }
            return metric;
        }

        private static void ValidateLength(IReadOnlyList<Word> words, int length, int alphabetPower) {
            foreach (Word word in words) {
                if (word.Count != length) {
                    throw new InvalidOperationException("Not all the words of the same length.");
                }
                foreach (int letter in word)
                {
                    if (letter < 0 || letter >= alphabetPower)
                    {
                        throw new InvalidOperationException("Out of alphabet letter.");
                    }
                }
            }
        }

        public static Word GetCandidate(IReadOnlyList<Word> words, IReadOnlyList<Word> attackWords, int alphabetPower)
        {
            if (words.Count == 1)
            {
                return words[0];
            }

            if (attackWords.Count <= 0)
            {
                throw new InvalidOperationException("No attack words.");
            }

            if (words.Count <= 0)
            {
                throw new InvalidOperationException("No hidden words.");
            }

            ValidateLength(words, words[0].Count, alphabetPower);
            ValidateLength(attackWords, words[0].Count, alphabetPower);

            ProgressBar progressBar = new("Main work ETA");
            long minMetric = long.MaxValue;
            int totalCount = attackWords.Count;
            Word candidateMin = attackWords[0];
            int count = 0;
            State state = new(candidateMin.Count, alphabetPower);
            for (int i = 0; i < attackWords.Count; ++i)
            {
                ++count;
                progressBar.Draw((double)count / totalCount);
                long currentMetric = 0;
                foreach (Word word in words)
                {
                    currentMetric = GetMatchWordCount(state, words, word, attackWords[i], currentMetric, minMetric);
                    if (currentMetric > minMetric)
                    {
                        break;
                    }
                }
                if (currentMetric < minMetric)
                {
                    candidateMin = attackWords[i];
                    minMetric = currentMetric;
                }
            }

            return candidateMin;
        }

        private static bool AllLettersUnique(Word word, bool[] letterCounter)
        {
            Array.Clear(letterCounter);
            foreach (int letter in word)
            {
                letterCounter[letter] = true;
            }
            int letterCount = 0;
            for (int i = 0; i < letterCounter.Length; ++i)
            {
                if (letterCounter[i])
                {
                    ++letterCount;
                }
            }
            return letterCount >= word.Count;
        }

        private static bool AllLettersUnique(Word first, Word second, bool[] letterCounter)
        {
            Array.Clear(letterCounter);
            foreach (int letter in first)
            {
                letterCounter[letter] = true;
            }
            foreach (int letter in second)
            {
                letterCounter[letter] = true;
            }
            int letterCount = 0;
            for (int i = 0; i < letterCounter.Length; ++i)
            {
                if (letterCounter[i])
                {
                    ++letterCount;
                }
            }
            return letterCount >= 2 * first.Count;
        }

        public static (Word firstGuess, Word secondGuess) GetCandidate2(IReadOnlyList<Word> words,
            IReadOnlyList<Word> attackWords, int alphabetPower)
        {
            if (words.Count == 1)
            {
                return (words[0], words[0]);
            }

            if (attackWords.Count <= 0)
            {
                throw new InvalidOperationException("No attack words.");
            }

            if (words.Count <= 0)
            {
                throw new InvalidOperationException("No hidden words.");
            }

            ValidateLength(words, words[0].Count, alphabetPower);
            ValidateLength(attackWords, words[0].Count, alphabetPower);

            ProgressBar progressBar = new("Main work ETA");
            long minMetric = long.MaxValue;
            long totalCount = 0;
            bool[] letters = new bool[alphabetPower];
            for (int i = 0; i < attackWords.Count; ++i)
            {
                if (!AllLettersUnique(attackWords[i], letters))
                {
                    continue;
                }
                for (int j = i + 1; j < attackWords.Count; ++j)
                {
                    if (AllLettersUnique(attackWords[i], attackWords[j], letters))
                    {
                        ++totalCount;
                    }
                }
            }

            (Word firstCandidate, Word secondCandidate) candidateMin = (attackWords[0], attackWords[0]);
            long count = 0;
            State firstState = new(words[0].Count, alphabetPower);
            State secondState = new(words[0].Count, alphabetPower);
            for (int i = 0; i < attackWords.Count; ++i)
            {
                if (!AllLettersUnique(attackWords[i], letters))
                {
                    continue;
                }
                for (int j = i + 1; j < attackWords.Count; ++j)
                {
                    if (!AllLettersUnique(attackWords[i], attackWords[j], letters))
                    {
                        continue;
                    }
                    ++count;
                    progressBar.Draw((double)count / totalCount);
                    long currentMetric = 0;
                    foreach (Word word in words)
                    {
                        currentMetric = GetMatchWordCount(firstState, secondState, words, word,
                            attackWords[i], attackWords[j], currentMetric, minMetric);
                        if (currentMetric > minMetric)
                        {
                            break;
                        }
                    }
                    if (currentMetric < minMetric)
                    {
                        candidateMin = (attackWords[i], attackWords[j]);
                        minMetric = currentMetric;
                    }
                }
            }

            return candidateMin;
        }
    }
}
