namespace FiveLetters
{
    public sealed class WordCollector
    {
        private readonly HashSet<string> words = [];

        private WordCollector() {}

        private void Process(ReadOnlyTree tree) {
            words.Add(tree.Word);
            foreach (ReadOnlyTree subtree in tree.Edges.Values) {
                Process(subtree);
            }
        }
        
        public static List<string> GetWords(ReadOnlyTree tree) {
            WordCollector wordCollector = new ();
            wordCollector.Process(tree);
            return wordCollector.words.Order().ToList();
        }
    }
}