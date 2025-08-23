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
            return value switch
            {
                'g' => EvaluationType.Absent,
                'w' => EvaluationType.Present,
                'y' => EvaluationType.Correct,
                _ => throw new ArgumentException(string.Format(
                                        "Value `{0}` contains at least one inacceptable " +
                                        "character. Expecting only the following characters: " +
                                        "`g`, `w`, `y`.", value)),
            };
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

        private static List<EvaluationType> GetEvaluations(IEnumerable<Data.Evaluation> evaluations)
        {
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

        private static ImmutableList<EvaluationType> Normalize(List<EvaluationType> evaluations, string guess)
        {
            if (guess.Length != evaluations.Count)
            {
                throw new ArgumentException(string.Format(
                    "Inconsistency: length of word and length of evaluations are different."));
            }

            Dictionary<char, int> presense = [];

            for (int i = 0; i < evaluations.Count; ++i)
            {
                if (evaluations[i] == EvaluationType.Present)
                {
                    char guessChar = guess[i];
                    if (presense.TryGetValue(guessChar, out int count))
                    {
                        presense[guessChar] = count + 1;
                    }
                    else
                    {
                        presense[guessChar] = 1;
                    }
                }
            }

            for (int i = 0; i < evaluations.Count; ++i)
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

            return [.. evaluations];
        }

        public Evaluation(string guess, string pattern)
        {
            evaluations = Normalize(GetEvaluations(pattern), guess);
        }

        public static Evaluation FromTwoWords(string guess, string hiddenWord)
        {
            if (hiddenWord.Length != guess.Length)
            {
                throw new InvalidOperationException("Word lengths are different.");
            }

            Dictionary<char, int> wordLetterCounter = [];

            for (int i = 0; i < hiddenWord.Length; ++i)
            {
                if (hiddenWord[i] != guess[i])
                {
                    wordLetterCounter[hiddenWord[i]] =
                        wordLetterCounter.GetValueOrDefault(hiddenWord[i], 0) + 1;                    
                }
            }

            List<EvaluationType> evaluations = [];
            for (int i = 0; i < hiddenWord.Length; ++i)
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
            return new([.. evaluations]);
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

        public static Evaluation Unpack(int value, string guess)
        {
            int count = Enum.GetValues<EvaluationType>().Length;
            List<EvaluationType> evaluationTypes = [];
            for (int i = 0; i < guess.Length; ++i)
            {
                evaluationTypes.Add((EvaluationType)(value % count));
                value /= count;
            }
            evaluationTypes.Reverse();
            return new(Normalize(evaluationTypes, guess));
        }

        public static Evaluation FromDataEvaluations(IReadOnlyList<Data.Evaluation> evaluations, string guess)
        {
            return new Evaluation(Normalize(GetEvaluations(evaluations), guess));
        }

        public IEnumerable<Data.Evaluation> ToDataEvaluations()
        {
            foreach (EvaluationType evaluationType in evaluations)
            {
                yield return ToDataEvaluation(evaluationType);
            }
        }

        private static Data.Evaluation ToDataEvaluation(EvaluationType evaluationType)
        {
            return evaluationType switch
            {
                EvaluationType.Absent => Data.Evaluation.Absent,
                EvaluationType.Correct => Data.Evaluation.Correct,
                EvaluationType.Present => Data.Evaluation.Present,
                _ => throw new InvalidOperationException(string.Format("Incorrect evaluation type: {0}.", evaluationType)),
            };
        }
    }
}
