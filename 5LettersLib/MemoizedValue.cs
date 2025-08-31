namespace FiveLetters
{
    public sealed class MemoizedValue<T>(Func<T> valueSupplier, TimeSpan expirationPeriod)
    {
        private readonly record struct Expirable
        {
            public T Value { get; init; }
            public DateTime ExpireAt { get; init; }
        }

        private Expirable? ExpirableValue = null;

        public T Get()
        {
            lock (this)
            {
                DateTime utcNow = DateTime.UtcNow;
                if (!ExpirableValue.HasValue || ExpirableValue.Value.ExpireAt <= utcNow)
                {
                    ExpirableValue = new Expirable
                    {
                        Value = valueSupplier(),
                        ExpireAt = utcNow + expirationPeriod
                    };
                }

                return ExpirableValue.Value.Value;
            }
        }
    }
}