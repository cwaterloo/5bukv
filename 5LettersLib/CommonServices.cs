using System.Collections.Immutable;
using System.Globalization;
using System.Net;
using System.Resources;
using System.Text;
using Protobuf.Text;
using FiveLetters.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.BotAPI;

namespace FiveLetters
{
    internal static class CommonServices
    {
        private static ReadOnlyTreeRoot GetTreeRoot(IServiceProvider serviceProvider)
        {
            return ReadOnlyTreeRoot.ValidateAndConvert(TreeSerializer.Load(serviceProvider.GetService<BotConfig>()!.TreeFilename!));
        }

        private static TelegramBotClient GetTelegramBotClient(IServiceProvider serviceProvider)
        {
            BotConfig config = serviceProvider.GetService<BotConfig>()!;
            if (string.IsNullOrEmpty(config.ProxyUrl))
            {
                return new(config.ApiToken!);
            }

            SocketsHttpHandler handler = new()
            {
                Proxy = new WebProxy(config.ProxyUrl),
                UseProxy = true
            };

            return new(new TelegramBotClientOptions(config.ApiToken!, new HttpClient(handler)));
        }

        private static CultureInfo GetCultureInfo(IServiceProvider serviceProvider)
        {
            return new CultureInfo(serviceProvider.GetService<BotConfig>()!.CultureName!, false);
        }

        private static BotConfig GetConfig(IServiceProvider serviceProvider)
        {
            IConfiguration configuration = serviceProvider.GetService<IConfiguration>()!;
            using StreamReader reader = new(configuration["config"]!, Encoding.UTF8);
            return new TextParser(TextParser.Settings.Default).Parse<BotConfig>(reader);
        }

        private static ImmutableSortedDictionary<int, int> GetStat(IServiceProvider serviceProvider)
        {
            return StatCollector.GetStat(serviceProvider.GetService<ReadOnlyTreeRoot>()!).ToImmutableSortedDictionary();
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
            return new(() => ConvertHelp(File.ReadAllLines(serviceProvider.GetService<BotConfig>()!.HelpTextFilePath!,
                Encoding.UTF8)), TimeSpan.FromMinutes(1));
        }

        public static IServiceCollection AddBotCommonServices(this IServiceCollection services)
        {
            services.AddSingleton(GetTreeRoot);
            services.AddSingleton(GetTelegramBotClient);
            services.AddSingleton(new ResourceManager("FiveLetters.Resources.Strings", typeof(BotApp).Assembly));
            services.AddSingleton(GetCultureInfo);
            services.AddSingleton<L10n>();
            services.AddSingleton(GetConfig);
            services.AddSingleton(GetStat);
            services.AddSingleton(GetHelp);
            return services;
        }
    }
}