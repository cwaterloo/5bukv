using System.Diagnostics;
using System.Globalization;

namespace FiveLetters
{
    public sealed class ProgressBar(string message)
    {
        private int state = -1;

        private readonly string message = message;

        private readonly Stopwatch stopwatch = Stopwatch.StartNew();

        private char StateChar
        {
            get
            {
                return state switch
                {
                    0 => '/',
                    1 => '-',
                    2 => '\\',
                    _ => '|',
                };
            }
        }

        public void Draw(double done)
        {
            if (state < 0)
            {
                state = 0;
            }
            else
            {
                int top = Console.GetCursorPosition().Top;
                Console.SetCursorPosition(0, top - 1);
            }

            ++state;
            if (state > 3)
            {
                state = 0;
            }

            Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "[{0}] {1}: {2}.", StateChar,
                message, DateTime.UtcNow + stopwatch.Elapsed * (1 / done - 1)));
        }
    }
}