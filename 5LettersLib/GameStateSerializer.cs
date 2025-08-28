using FiveLetters.Data;
using Google.Protobuf;

namespace FiveLetters
{
    internal static class GameStateSerializer
    {
        internal static GameState Load(string value)
        {
            using MemoryStream memoryStream = new(Convert.FromBase64String(value));
            using CodedInputStream codedInputStream = new(memoryStream);
            return GameState.Parser.ParseFrom(codedInputStream);
        }

        internal static string Save(GameState state) {
            using MemoryStream memoryStream = new();
            using (CodedOutputStream codedOutputStream = new(memoryStream)) {
                state.WriteTo(codedOutputStream);
            }
            memoryStream.Flush();
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
}
