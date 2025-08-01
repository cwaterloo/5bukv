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

        private static long GetMatchWordCount(List<Word> words, Word word, Word guess, long current, long observedMin)
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

        public static Word GetCandidate(List<Word> words, List<Word> attackWords)
        {
            if (words.Count == 1)
            {
                return words[0];
            }

            ProgressBar progressBar = new("Main work ETA");
            long minMetric = long.MaxValue;
            int totalCount = attackWords.Count;
            Word? candidateMin = null;
            int count = 0;
            for (int i = 0; i < attackWords.Count; ++i)
            {
                ++count;
                progressBar.Draw((double)count / totalCount);
                long currentMetric = 0;
                foreach (Word word in words)
                {
                    currentMetric = GetMatchWordCount(words, word, attackWords[i], currentMetric, minMetric);
                    if (currentMetric > minMetric)
                    {
                        break;
                    }
                }
                if (currentMetric < minMetric)
                {
                    candidateMin = attackWords[i];
                    minMetric = currentMetric;
                }
            }

            if (candidateMin.HasValue)
            {
                return candidateMin.Value;
            }

            throw new InvalidOperationException("No more words left.");
        }

        public static (Word guess1, Word guess2) GetCandidate2(List<Word> words, List<Word> attackWords)
        {
            if (words.Count == 1)
            {
                return (words[0], words[0]);
            }

            ProgressBar progressBar = new("Main work ETA");
            long minMetric = long.MaxValue;
            long totalCount = attackWords.Count * (attackWords.Count - 1) / 2;
            (Word candidate1, Word candidate2)? candidateMin = null;
            long count = 0;
            for (int i = 0; i < attackWords.Count; ++i)
            {
                for (int j = i + 1; j < attackWords.Count; ++j)
                {
                    ++count;
                    progressBar.Draw((double)count / totalCount);
                    long currentMetric = 0;
                    foreach (Word word in words)
                    {
                        currentMetric = GetMatchWordCount(words, word, attackWords[i], attackWords[j], currentMetric, minMetric);
                        if (currentMetric > minMetric)
                        {
                            break;
                        }
                    }
                    if (currentMetric < minMetric)
                    {
                        candidateMin = (attackWords[i], attackWords[j]);
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
