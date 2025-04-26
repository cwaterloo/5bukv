using System.Diagnostics;
using System.Globalization;

namespace FiveLetters
{
    internal sealed class AI
    {
        internal static long GetMatchWordCount(List<Word> words, Word word, Word guess, long current, long observedMin)
        {
            long metric = current;
            long n = 0;
            long k = 0;
            IState state = StateFactory.Make(word, guess);
            foreach (Word wordToCheck in words)
            {
                if (state.MatchWord(wordToCheck))
                {
                    metric += 3 * (n + k) + 1;
                    k += (n << 1) + 1;
                    ++n;
                    if (metric > observedMin)
                    {
                        return metric;
                    }
                }
            }
            return metric;
        }

        internal static Word GetCandidate(List<Word> words, List<Word> globalWords)
        {
            if (words.Count == 1)
            {
                return words[0];
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            long minMetric = long.MaxValue;
            Word? candidateMin = null;
            for (int i = 0; i < globalWords.Count; ++i)
            {
                long currentMetric = 0;
                foreach (Word word in words)
                {
                    currentMetric = GetMatchWordCount(words, word, globalWords[i], currentMetric, minMetric);
                    if (currentMetric > minMetric)
                    {
                        break;
                    }
                }
                if (currentMetric < minMetric)
                {
                    candidateMin = globalWords[i];
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
