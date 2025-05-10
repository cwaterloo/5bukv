using System.IO.Compression;
using FiveLetters.Data;
using Google.Protobuf;

namespace FiveLetters
{
    public static class TreeSerializer
    {
        public static void Save(Tree tree, string outputFilename)
        {
            using FileStream fileStream = new(outputFilename, FileMode.Create);
            using GZipStream gZipStream = new(fileStream, CompressionLevel.SmallestSize);
            using CodedOutputStream codedOutputStream = new(gZipStream);
            tree.WriteTo(codedOutputStream);
        }

        public static Tree Load(string inputFileName) {
            using FileStream fileStream = new(inputFileName, FileMode.Open);
            using GZipStream gZipStream = new(fileStream, CompressionMode.Decompress);
            using CodedInputStream codedInputStream = new(gZipStream);
            return Tree.Parser.ParseFrom(codedInputStream);
        }
    }
}