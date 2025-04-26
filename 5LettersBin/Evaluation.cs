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
