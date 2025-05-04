using FiveLetters.Data;

namespace FiveLetters
{
    internal sealed class TreeGenerator
    {
        private List<Word> globalWords;

        private TreeGenerator(List<Word> globalWords) {
            this.globalWords = globalWords;
        }

        internal static Tree Get(List<Word> globalWords)
        {
            if (globalWords.Count <= 0) {
                throw new ArgumentException("List of words must not be empty.");
            }
            return new TreeGenerator(globalWords).Make(globalWords);
        }

        private Tree Make(List<Word> candidates)
        {
            Word guess = AI.GetCandidate(candidates, globalWords);
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
