using System.Collections.Immutable;

namespace FiveLetters
{
    internal sealed class Evaluation
    {
        internal enum EvaluationType
        {
            Absent,
            Present,
            Correct
        }

        private readonly ImmutableList<EvaluationType> evaluations;

        private static EvaluationType GetEvaluation(char value) {
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

        private static List<EvaluationType> GetEvaluations(string value) {
            List<EvaluationType> evaluations = [];

            foreach (char chr in value)
            {
                evaluations.Add(GetEvaluation(chr));
            }

            return evaluations;
        }

        internal Evaluation(Word guess, string value) {
            if (value.Length != Word.WordLetterCount)
            {
                throw new ArgumentException(string.Format(
                    "The mask must contain exactly {0} characters.", Word.WordLetterCount));
            }
            List<EvaluationType> evaluations = GetEvaluations(value);

            HashSet<int> presentLetters = guess.Zip(evaluations)
                .Where(pair => pair.Second != EvaluationType.Absent)
                .Select(pair => pair.First.Value).ToHashSet();

            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                if (evaluations[i] == EvaluationType.Absent && presentLetters.Contains(guess[i]))
                {
                    evaluations[i] = EvaluationType.Present;
                }
            }
            this.evaluations = evaluations.ToImmutableList();
        }

        internal Evaluation(Word hiddenWord, Word guess)
        {
            if (hiddenWord.AlphabetLetterCount != guess.AlphabetLetterCount)
            {
                throw new InvalidOperationException("An attempt to create a state from different alphabet length.");
            }

            HashSet<int> presentLetters = hiddenWord.Select(letter => letter.Value).ToHashSet();
            List<EvaluationType> evaluations = [];
            for (int i = 0; i < Word.WordLetterCount; ++i)
            {
                if (hiddenWord[i] == guess[i])
                {
                    evaluations.Add(EvaluationType.Correct);
                }
                else
                {
                    evaluations.Add(presentLetters.Contains(guess[i].Value) ? EvaluationType.Present : EvaluationType.Absent);
                }
            }
            this.evaluations = evaluations.ToImmutableList();
        }

        internal Evaluation(ImmutableList<EvaluationType> evaluations)
        {
            this.evaluations = evaluations;
        }

        internal int Pack()
        {
            int count = Enum.GetValues<EvaluationType>().Length;
            int result = 0;
            foreach (EvaluationType evaluationType in evaluations)
            {
                result = result * count + (int)evaluationType;
            }
            return result;
        }
    }
}
