using FiveLetters.Data;

namespace FiveLetters
{
    public sealed class ReadOnlyTreeRoot
    {
        public static ReadOnlyTreeRoot ValidateAndConvert(Tree tree)
        {
            int length = tree.Word.Length;

            ValidateWordLength(tree, length);

            if (length < 1 || length > 19)
            {
                throw new InvalidOperationException("Expected word length range is 1..19.");
            }

            int maxStateValue = (int)(Math.Pow(Enum.GetValues<EvaluationType>().Length, length) + 0.5);

            (int min, int max) = GetStateMinMax(tree);

            if (min < 0 || max >= maxStateValue)
            {
                throw new InvalidOperationException(string.Format(
                    "Expected min value of state is 0 and max value of state is {0}.", maxStateValue));
            }

            ValidateStateValues(tree);
            return new ReadOnlyTreeRoot(length, Convert(tree));
        }

        private static void ValidateWordLength(Tree tree, int length)
        {
            if (tree.Word.Length != length)
            {
                throw new InvalidOperationException("Not all the words of the same length.");
            }
            foreach ((int state, Tree subtree) in tree.Edges)
            {
                ValidateWordLength(subtree, length);
            }
        }

        private static ReadOnlyTree Convert(Tree tree)
        {
            Dictionary<int, ReadOnlyTree> subtrees = [];

            foreach ((int state, Tree subtree) in tree.Edges)
            {
                subtrees.Add(state, Convert(subtree));
            }

            return new ReadOnlyTree(tree.Word, subtrees.AsReadOnly());
        }

        private static void ValidateStateValues(Tree tree)
        {
            foreach ((int state, Tree subtree) in tree.Edges)
            {
                if (state != Evaluation.Unpack(state, tree.Word).Pack())
                {
                    throw new InvalidOperationException("Inconsistent pack and unpack values.");
                }
                ValidateStateValues(subtree);
            }
        }

        private static (int min, int max) GetStateMinMax(Tree tree)
        {
            if (tree.Edges.Count <= 0)
            {
                return (0, 0);
            }

            int min = int.MaxValue;
            int max = int.MinValue;
            foreach ((int state, Tree subtree) in tree.Edges)
            {
                min = Math.Min(min, state);
                max = Math.Max(max, state);
                (int submin, int submax) = GetStateMinMax(subtree);
                min = Math.Min(min, submin);
                max = Math.Max(max, submax);
            }

            return (min, max);
        }

        private ReadOnlyTreeRoot(int wordLength, ReadOnlyTree tree)
        {
            WordLength = wordLength;
            Tree = tree;
        }

        public int WordLength { get; init; }

        public ReadOnlyTree Tree { get; init; }
    }

    public sealed record ReadOnlyTree(string Word, IReadOnlyDictionary<int, ReadOnlyTree> Edges);
}