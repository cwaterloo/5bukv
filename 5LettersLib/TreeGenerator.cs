using FiveLetters.Data;

namespace FiveLetters
{
    public sealed class TreeGenerator
    {
        private readonly List<Word> attackWords;

        private TreeGenerator(List<Word> attackWords)
        {
            this.attackWords = attackWords;
        }

        public static Tree Get(List<Word> globalWords, List<Word> attackWords)
        {
            if (globalWords.Count <= 0 || attackWords.Count <= 0)
            {
                throw new ArgumentException("List of words must not be empty.");
            }
            return new TreeGenerator(attackWords).Make(globalWords);
        }

        private Tree Make(List<Word> candidates)
        {
            Word guess = AI.GetCandidate(candidates, attackWords);
            Dictionary<int, List<Word>> stateWords = [];
            foreach (Word hiddenWord in candidates)
            {
                int packedState = new Evaluation(hiddenWord, guess).Pack();
                List<Word> words = stateWords.TryGetValue(packedState, out List<Word>? value) ? value : stateWords[packedState] = [];
                words.Add(hiddenWord);
            }

            Dictionary<int, Tree> edges = stateWords.Count == 1 ? [] : stateWords.ToDictionary(keyValue => keyValue.Key, keyValue => Make(keyValue.Value));
            return new()
            {
                Word = guess.ToString(),
                Edges = { edges }
            };
        }
    }
}
