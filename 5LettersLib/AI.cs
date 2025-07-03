using System.Diagnostics;

namespace FiveLetters
{
    public static class AI
    {
        private static long GetMatchWordCount(List<Word> words, Word word, Word guess1, Word guess2, long current, long observedMin)
        {
            long metric = current;
            long n = 0;
            State state1 = new(word, guess1);
            State state2 = new(word, guess2);
            foreach (Word wordToCheck in words)
            {
                if (state1.MatchWord(wordToCheck) && state2.MatchWord(wordToCheck))
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

        public static long GetMatchWordCount(List<Word> words, Word word, Word guess, long current, long observedMin)
        {
            long metric = current;
            long n = 0;
            State state = new(word, guess);
            foreach (Word wordToCheck in words)
            {
                if (state.MatchWord(wordToCheck))
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

        public static Word GetCandidate(List<Word> words, List<Word> globalWords)
        {
            if (words.Count == 1)
            {
                return words[0];
            }

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
            }

            if (candidateMin.HasValue)
            {
                return candidateMin.Value;
            }

            throw new InvalidOperationException("No more words left.");
        }

        public static (Word guess1, Word guess2) GetCandidate2(List<Word> words, List<Word> globalWords)
        {
            if (words.Count == 1)
            {
                return (words[0], words[0]);
            }

            //Stopwatch watch = Stopwatch.StartNew();
            long minMetric = long.MaxValue;
            //long totalCount = globalWords.Count * (globalWords.Count - 1) / 2;
            (Word candidate1, Word candidate2)? candidateMin = null;
            //long count = 0;
            for (int i = 0; i < globalWords.Count; ++i)
            {
                for (int j = i + 1; j < globalWords.Count; ++j)
                {
                    //++count;
                //    Console.WriteLine("ETA: {0}.", DateTime.UtcNow + watch.Elapsed * ((double)totalCount / count - 1.0));
                    long currentMetric = 0;
                    foreach (Word word in words)
                    {
                        currentMetric = GetMatchWordCount(words, word, globalWords[i], globalWords[j], currentMetric, minMetric);
                        if (currentMetric > minMetric)
                        {
                            break;
                        }
                    }
                    if (currentMetric < minMetric)
                    {
                        candidateMin = (globalWords[i], globalWords[j]);
                        minMetric = currentMetric;
                    }
                }
            }

            if (candidateMin.HasValue)
            {
                return candidateMin.Value;
            }

            throw new InvalidOperationException("No more words left.");
        }
    }
}
