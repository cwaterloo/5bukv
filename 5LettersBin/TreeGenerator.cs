using FiveLetters.Data;

namespace FiveLetters
{
    internal sealed class TreeGenerator(List<Word> globalWords)
    {
        internal static Tree Get(List<Word> globalWords)
        {
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
