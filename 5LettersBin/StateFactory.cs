namespace FiveLetters
{
    internal static class StateFactory
    {
        internal static IState Make(Word word, Word guess)
        {
            return new OldState(word, guess);
        }

        internal static IState Make(string value, Word guess)
        {
            return new OldState(value, guess);
        }
    }
}
