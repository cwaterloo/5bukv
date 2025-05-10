using System.IO.Compression;
using FiveLetters.Data;
using Google.Protobuf;

namespace FiveLetters
{
    public static class GameStateSerializer
    {
        public static GameState Load(string value)
        {
            using MemoryStream memoryStream = new(Convert.FromBase64String(value));
            using GZipStream gZipStream = new(memoryStream, CompressionMode.Decompress);
            using CodedInputStream codedInputStream = new(gZipStream);
            return GameState.Parser.ParseFrom(codedInputStream);
        }

        public static string Save(GameState state) {
            using MemoryStream memoryStream = new();
            using GZipStream gZipStream = new(memoryStream, CompressionLevel.SmallestSize);
            using CodedOutputStream codedOutputStream = new(memoryStream);
            state.WriteTo(codedOutputStream);
            codedOutputStream.Flush();
            return Convert.ToBase64String(memoryStream.ToArray());
        }
    }
}
