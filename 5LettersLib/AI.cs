namespace FiveLetters
{
    public static class AI
    {
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
    }
}
