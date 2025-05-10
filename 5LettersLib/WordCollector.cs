using FiveLetters.Data;

namespace FiveLetters
{
    public sealed class WordCollector
    {
        private readonly HashSet<string> words = [];

        private WordCollector() {}

        private void Process(Tree tree) {
            words.Add(tree.Word);
            foreach (Tree subtree in tree.Edges.Values) {
                Process(subtree);
            }
        }
        
        public static List<string> GetWords(Tree tree) {
            WordCollector wordCollector = new ();
            wordCollector.Process(tree);
            return wordCollector.words.Order().ToList();
        }
    }
}