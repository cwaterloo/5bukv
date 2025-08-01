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

        public static Tree Get(List<Word> globalWords, List<Word> attackWords, bool dual)
        {
            if (globalWords.Count <= 0 || attackWords.Count <= 0)
            {
                throw new ArgumentException("List of words must not be empty.");
            }
            TreeGenerator generator = new(attackWords);
            return dual ? generator.Make2(globalWords, 0) : generator.Make(globalWords, 0);
        }

        private Tree Make(List<Word> candidates, int level)
        {
            Word guess = AI.GetCandidate(candidates, attackWords);
            Dictionary<int, List<Word>> stateWords = [];
            foreach (Word hiddenWord in candidates)
            {
                int packedState = new Evaluation(hiddenWord, guess).Pack();
                List<Word> words = stateWords.TryGetValue(packedState, out List<Word>? value) ? value : stateWords[packedState] = [];
                words.Add(hiddenWord);
            }

            Dictionary<int, Tree> edges = stateWords.Count == 1 ? [] : stateWords.ToDictionary(keyValue => keyValue.Key, keyValue => Make(keyValue.Value, level + 1));
            return new()
            {
                Word = guess.ToString(),
                Edges = { edges }
            };
        }

        private Tree Make2(List<Word> candidates, int level)
        {
            (Word guess1, Word guess2) = AI.GetCandidate2(candidates, attackWords);
            Dictionary<int, Dictionary<int, List<Word>>> stateWords = [];
            foreach (Word hiddenWord in candidates)
            {
                int packedState1 = new Evaluation(hiddenWord, guess1).Pack();
                Dictionary<int, List<Word>> words1 = stateWords.TryGetValue(packedState1, out Dictionary<int, List<Word>>? value1) ? value1 : stateWords[packedState1] = [];
                int packedState2 = new Evaluation(hiddenWord, guess2).Pack();
                List<Word> words2 = words1.TryGetValue(packedState2, out List<Word>? value2) ? value2 : words1[packedState2] = [];
                words2.Add(hiddenWord);
            }

            string guess2Str = guess2.ToString();
            Dictionary<int, Tree> edges = [];
            foreach (KeyValuePair<int, Dictionary<int, List<Word>>> keyValue1 in stateWords)
            {
                Dictionary<int, List<Word>> value1 = keyValue1.Value;
                if (value1.Values.Count == 1)
                {
                    List<Word> words = value1.Values.Single();
                    if (words.Count == 1)
                    {
                        edges.Add(keyValue1.Key, Make(words, level + 1));
                        continue;
                    }
                }

                edges.Add(keyValue1.Key, new Tree { Word = guess2Str, Edges = {
                    keyValue1.Value.ToDictionary(keyValue2 => keyValue2.Key, keyValue2 => Make(keyValue2.Value, level + 2)) } });
            }

            return new()
            {
                Word = guess1.ToString(),
                Edges = { edges }
            };
        }
    }
}
