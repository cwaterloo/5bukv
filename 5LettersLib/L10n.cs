using System.Globalization;
using System.Resources;

namespace FiveLetters
{
    public sealed class L10n(CultureInfo cultureInfo, ResourceManager resourceManager)
    {
        public string GetResourceString(string key)
        {
            return resourceManager.GetString(key, cultureInfo)!;
        }
    }
}