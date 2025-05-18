using System.Net;

namespace FiveLetters
{
    public record Config
    {
        public string? ApiToken { get; init; }
        public string? SecretToken { get; init; }
        public Uri? WebHookUrl { get; init; }
        public string? PublicKeyFilename { get; init; }
        public IPAddress? IPAddress { get; init; }
        public string? CultureName { get; init; }
        public string? TreeFilename { get; init; }        
        public string? PathPattern { get; init; }
    }
}
