using System.Collections.Immutable;
using FiveLetters.Data;

namespace FiveLetters
{
    public sealed class StatCollector
    {
        private readonly List<string> stack = [];
        private readonly Action<ImmutableList<string>> action;

        private StatCollector(Action<ImmutableList<string>> action)
        {
            this.action = action;
        }

        private void TraverseChains(ReadOnlyTree tree)
        {
            stack.Add(tree.Word);
            if (tree.Edges.Count == 0)
            {
                action(stack.ToImmutableList());
            }

            foreach (ReadOnlyTree subtree in tree.Edges.Values)
            {
                TraverseChains(subtree);
            }

            stack.RemoveAt(stack.Count - 1);
        }

        private static void HandleChain(ImmutableList<string> chain, SortedDictionary<int, int> result)
        {
            if (chain.Count == 0)
            {
                IncreaseOrSetDefault(result, 0);
                return;
            }

            int index = chain.Count - 1;
            string word = chain[index];

            for (int i = 0; i < chain.Count - 1; ++i)
            {
                if (chain[i] == word)
                {
                    index = i;
                    break;
                }
            }

            IncreaseOrSetDefault(result, index + 1);
        }

        public static SortedDictionary<int, int> GetStat(ReadOnlyTreeRoot tree)
        {
            SortedDictionary<int, int> result = [];
            StatCollector statCollector = new StatCollector(chain => HandleChain(chain, result));
            statCollector.TraverseChains(tree.Tree);
            return result;
        }

        private static void IncreaseOrSetDefault(SortedDictionary<int, int> map, int key) {
            if (map.TryGetValue(key, out int count))
            {
                map[key] = ++count;
            }
            else
            {
                map[key] = 1;
            }
        }
    }
}