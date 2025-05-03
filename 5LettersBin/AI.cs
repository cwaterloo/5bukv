using System.Diagnostics;
using System.Globalization;

namespace FiveLetters
{
    internal sealed class AI
    {
        internal static long GetMatchWordCount(List<int> words, int word, int guess, long current, long observedMin, List<Word> globalWords)
        {
            long metric = current;
            long n = 0;
            IState state = StateFactory.Make(globalWords[word], globalWords[guess]);
            foreach (int wordToCheck in words)
            {
                if (state.MatchWord(globalWords[wordToCheck]))
                {
                    metric += (n << 1) + 1;
                    ++n;
                    if (metric > observedMin)
                    {
                        return metric;
                    }
                }
            }
            return metric;
        }

        internal static int GetCandidate(List<int> words, List<Word> globalWords)
        {
            if (words.Count == 1)
            {
                return words[0];
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            long minMetric = long.MaxValue;
            int? candidateMin = null;
            foreach (int i in Enumerable.Range(0, globalWords.Count))
            {
                long currentMetric = 0;
                foreach (int word in words)
                {
                    currentMetric = GetMatchWordCount(words, word, i, currentMetric, minMetric, globalWords);
                    if (currentMetric > minMetric)
                    {
                        break;
                    }
                }
                if (currentMetric < minMetric)
                {
                    candidateMin = i;
                    minMetric = currentMetric;
                }

                long elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                if (elapsedMilliseconds > 60000)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "GetCandidate ETA: {0}",
                        DateTime.Now.AddMilliseconds(elapsedMilliseconds * (globalWords.Count - i - 1) / (i + 1))));
                }
            }

            stopwatch.Stop();

            if (candidateMin.HasValue)
            {
                return candidateMin.Value;
            }

            throw new InvalidOperationException("No more words left.");
        }
    }
}
