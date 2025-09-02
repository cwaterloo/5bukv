namespace FiveLetters
{
    internal sealed class TupleGenerator
    {
        private readonly StreamWriter streamWriter;

        private readonly IReadOnlyList<string> words;

        private readonly List<string> chain = [];

        private long count = 0;

        internal static long Generate(IReadOnlyList<string> words, int tupleSize, StreamWriter streamWriter)
        {
            TupleGenerator tupleGenerator = new(streamWriter, words);
            tupleGenerator.Make(tupleSize, 0);
            return tupleGenerator.count;
        }

        private void WriteChain(IReadOnlyList<string> chain)
        {
            streamWriter.WriteLine(string.Join(' ', chain));
        }

        private void Make(int tupleSize, int index)
        {
            if (tupleSize <= 0)
            {
                ++count;
                WriteChain(chain);
                return;
            }

            int length = chain.Select(word => word.Length).Sum();

            for (int i = index; i < words.Count; ++i)
            {
                if (words[i].Concat(chain.SelectMany(word => word)).Distinct().Count() < length + words[i].Length)
                {
                    continue;
                }

                chain.Add(words[i]);
                Make(tupleSize - 1, i + 1);
                chain.RemoveAt(chain.Count - 1);
            }
        }

        private TupleGenerator(StreamWriter streamWriter, IReadOnlyList<string> words)
        {
            this.streamWriter = streamWriter;
            this.words = words;
        }
    }
}
