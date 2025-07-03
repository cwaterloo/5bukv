using System.Collections.Immutable;

namespace FiveLetters
{
    public enum EvaluationType
    {
        Absent,
        Present,
        Correct
    }

    public sealed class Evaluation
    {
        private readonly ImmutableList<EvaluationType> evaluations;

        private static EvaluationType GetEvaluation(char value)
        {
            switch (value)
            {
                case 'g':
                    return EvaluationType.Absent;
                case 'w':
                    return EvaluationType.Present;
                case 'y':
                    return EvaluationType.Correct;
                default:
                    throw new ArgumentException(string.Format(
                        "Value `{0}` contains at least one inacceptable " +
                        "character. Expecting only the following characters: " +
                        "`g`, `w`, `y`.", value));
            }
        }

        private static List<EvaluationType> GetEvaluations(string value)
        {
            List<EvaluationType> evaluations = [];

            foreach (char chr in value)
            {
                evaluations.Add(GetEvaluation(chr));
            }

            return evaluations;
        }

        private static List<EvaluationType> GetEvaluations(IEnumerable<Data.Evaluation> evaluations) {
            List<EvaluationType> evaluationTypes = [];
            foreach (Data.Evaluation evaluation in evaluations)
            {
                switch (evaluation)
                {
                    case Data.Evaluation.Absent:
                        evaluationTypes.Add(EvaluationType.Absent);
                        break;
                    case Data.Evaluation.Correct:
                        evaluationTypes.Add(EvaluationType.Correct);
                        break;
                    case Data.Evaluation.Present:
                        evaluationTypes.Add(EvaluationType.Present);
                        break;
                }
            }

            return evaluationTypes;
        }

        private static void Increment<T>(Dictionary<T, int> counters, T letter) where T : notnull
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

        public Evaluation(Word guess, string value)
        {
            if (value.Length != Word.WordLetterCount)
            {
                throw new ArgumentException(string.Format(
                    "The mask must contain exactly {0} characters.", Word.WordLetterCount));
            }
            List<EvaluationType> evaluations = GetEvaluations(value);

            Dictionary<Letter, int> presense = [];

            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                if (evaluations[i] == EvaluationType.Present)
                {
                    Increment(presense, guess[i]);
                }
            }

            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                if (evaluations[i] == EvaluationType.Correct)
                {
                    continue;
                }
                if (presense.GetValueOrDefault(guess[i], 0) > 0)
                {
                    evaluations[i] = EvaluationType.Present;
                    --presense[guess[i]];
                }
                else
                {
                    evaluations[i] = EvaluationType.Absent;
                }
            }

            this.evaluations = evaluations.ToImmutableList();
        }

        public Evaluation(Word hiddenWord, Word guess)
        {
            if (hiddenWord.AlphabetLetterCount != guess.AlphabetLetterCount)
            {
                throw new InvalidOperationException("An attempt to create a state from different alphabet length.");
            }

            Dictionary<Letter, int> wordLetterCounter = []; 

            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                if (hiddenWord[i] != guess[i])
                {
                    Increment(wordLetterCounter, hiddenWord[i]);
                }
            }

            List<EvaluationType> evaluations = [];
            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                if (hiddenWord[i] == guess[i])
                {
                    evaluations.Add(EvaluationType.Correct);
                }
                else if (wordLetterCounter.GetValueOrDefault(guess[i], 0) > 0)
                {
                    evaluations.Add(EvaluationType.Present);
                    --wordLetterCounter[guess[i]];
                }
                else
                {
                    evaluations.Add(EvaluationType.Absent);
                }
            }
            this.evaluations = evaluations.ToImmutableList();
        }

        private Evaluation(ImmutableList<EvaluationType> evaluations)
        {
            this.evaluations = evaluations;
        }

        public int Pack()
        {
            int count = Enum.GetValues<EvaluationType>().Length;
            int result = 0;
            foreach (EvaluationType evaluationType in evaluations)
            {
                result = result * count + (int)evaluationType;
            }
            return result;
        }

        public static Evaluation Unpack(int value)
        {
            int count = Enum.GetValues<EvaluationType>().Length;
            List<EvaluationType> evaluationTypes = [];
            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                evaluationTypes.Add((EvaluationType)(value % count));
                value /= count;
            }
            evaluationTypes.Reverse();
            return new(evaluationTypes.ToImmutableList());
        }

        public static Evaluation FromDataEvaluations(IEnumerable<Data.Evaluation> evaluations, string word)
        {
            if (word.Length != Word.WordLetterCount)
            {
                throw new ArgumentException(string.Format(
                    "The word must contain exactly {0} characters.", Word.WordLetterCount));
            }

            List<EvaluationType> evaluationTypes = GetEvaluations(evaluations);

            Dictionary<char, int> presense = [];

            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                if (evaluationTypes[i] == EvaluationType.Present)
                {
                    Increment(presense, word[i]);
                }
            }

            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                if (evaluationTypes[i] == EvaluationType.Correct)
                {
                    continue;
                }
                if (presense.GetValueOrDefault(word[i], 0) > 0)
                {
                    evaluationTypes[i] = EvaluationType.Present;
                    --presense[word[i]];
                }
                else
                {
                    evaluationTypes[i] = EvaluationType.Absent;
                }
            }

            return new Evaluation(evaluationTypes.ToImmutableList());
        }

        public IEnumerable<Data.Evaluation> ToDataEvaluations()
        {
            foreach (EvaluationType evaluationType in evaluations)
            {
                switch (evaluationType)
                {
                    case EvaluationType.Absent:
                        yield return Data.Evaluation.Absent;
                        break;
                    case EvaluationType.Correct:
                        yield return Data.Evaluation.Correct;
                        break;
                    case EvaluationType.Present:
                        yield return Data.Evaluation.Present;
                        break;
                }
            }
        }
    }
}
