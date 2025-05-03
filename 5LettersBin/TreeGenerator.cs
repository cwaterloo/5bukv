using FiveLetters.Data;

namespace FiveLetters
{
    internal sealed class TreeGenerator
    {
        private List<Word> globalWords;

        private TreeGenerator(List<Word> globalWords) {
            this.globalWords = globalWords;
        }

        internal static Navigation Get(List<Word> globalWords)
        {
            if (globalWords.Count <= 0) {
                throw new ArgumentException("List of words must not be empty.");
            }

            return new Navigation {
                Word = { globalWords.Select(word => word.ToString()) },
                Tree = new TreeGenerator(globalWords).Make(Enumerable.Range(0, globalWords.Count).ToList())
            };
        }

        private Tree Make(List<int> candidates)
        {
            int guess = AI.GetCandidate(candidates, globalWords);
            Dictionary<int, List<int>> stateWords = [];
            foreach (int hiddenWord in candidates)
            {
                int packedState = new Evaluation(globalWords[hiddenWord], globalWords[guess]).Pack();
                List<int> words = stateWords.TryGetValue(packedState, out List<int>? value) ? value : stateWords[packedState] = [];
                words.Add(hiddenWord);
            }

            Dictionary<int, Tree> edges = stateWords.Count == 1 ? [] : stateWords.ToDictionary(keyValue => keyValue.Key, keyValue => Make(keyValue.Value));
            return new()
            {
                Word = guess,
                Edges = { edges }
            };
        }
    }
}
