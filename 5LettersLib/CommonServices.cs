using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Resources;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.BotAPI;

namespace FiveLetters
{
    internal static class CommonServices
    {
        private static ReadOnlyTreeRoot GetTreeRoot(IServiceProvider serviceProvider)
        {
            return ReadOnlyTreeRoot.ValidateAndConvert(TreeSerializer.Load(serviceProvider.GetService<Config>()!.TreeFilename!));
        }

        private static TelegramBotClient GetTelegramBotClient(IServiceProvider serviceProvider)
        {
            return new(serviceProvider.GetService<Config>()!.ApiToken!);
        }

        private static CultureInfo GetCultureInfo(IServiceProvider serviceProvider)
        {
            return new CultureInfo(serviceProvider.GetService<Config>()!.CultureName!, false);
        }

        private static Config GetConfig(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>()!;
            IConfigurationSection appSettings = configuration.GetSection("AppSettings");
            return new Config
            {
                ApiToken = appSettings.GetValue<string>("ApiToken"),
                WebHookUrl = GetUri(appSettings.GetValue<string>("WebHookUrl")),
                SecretToken = appSettings.GetValue<string>("SecretToken"),
                PublicKeyFilename = appSettings.GetValue<string>("PublicKeyFilename"),
                IPAddress = GetIPAddress(appSettings.GetValue<string>("IPAddress")),
                CultureName = appSettings.GetValue<string>("CultureName"),
                TreeFilename = appSettings.GetValue<string>("TreeFilename"),
                PathPattern = appSettings.GetValue<string>("PathPattern"),
                FeedbackEmail = appSettings.GetValue<string>("FeedbackEmail"),
                HelpTextFilePath = appSettings.GetValue<string>("HelpTextFilePath")
            };
        }

        private static ImmutableSortedDictionary<int, int> GetStat(IServiceProvider serviceProvider)
        {
            return StatCollector.GetStat(serviceProvider.GetService<ReadOnlyTreeRoot>()!).ToImmutableSortedDictionary();
        }

        private static IPAddress? GetIPAddress(string? value)
        {
            if (value == null)
            {
                return null;
            }

            return IPAddress.Parse(value);
        }

        private static Uri? GetUri(string? value)
        {
            if (value == null)
            {
                return null;
            }

            return new Uri(value);
        }

        private static string ConvertHelp(string[] helpLines)
        {
            StringBuilder builder = new();
            bool lastNewLine = true;
            foreach (string helpLine in helpLines)
            {
                if (helpLine == "")
                {
                    builder.Append('\n');
                    lastNewLine = true;
                }
                else
                {
                    if (lastNewLine)
                    {
                        lastNewLine = false;
                    }
                    else
                    {
                        builder.Append(' ');
                    }
                    builder.Append(helpLine);
                }
            }
            return builder.ToString();
        }

        private static MemoizedValue<string> GetHelp(IServiceProvider serviceProvider)
        {
            return new(() => ConvertHelp(File.ReadAllLines(serviceProvider.GetService<Config>()!.HelpTextFilePath!,
                Encoding.UTF8)), TimeSpan.FromMinutes(1));
        }

        public static void AddBotCommonServices(this IServiceCollection services)
        {
            services.AddSingleton(GetTreeRoot);
            services.AddSingleton(GetTelegramBotClient);
            services.AddSingleton(new ResourceManager("FiveLetters.Resources.Strings", typeof(BotApp).Assembly));
            services.AddSingleton(GetCultureInfo);
            services.AddSingleton<L10n>();
            services.AddSingleton(GetConfig);
            services.AddSingleton(GetStat);
            services.AddSingleton(GetHelp);
        }
    }
}