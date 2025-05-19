using System.Globalization;
using System.Net;
using System.Resources;
using FiveLetters.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.BotAPI;

namespace FiveLetters
{
    public static class CommonServices
    {
        private static Tree GetTree(IServiceProvider serviceProvider)
        {
            return TreeSerializer.Load(serviceProvider.GetService<Config>()!.TreeFilename!);
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
            return new Config
            {
                ApiToken = configuration.GetSection("AppSettings").GetValue<string>("ApiToken"),
                WebHookUrl = GetUri(configuration.GetSection("AppSettings").GetValue<string>("WebHookUrl")),
                SecretToken = configuration.GetSection("AppSettings").GetValue<string>("SecretToken"),
                PublicKeyFilename = configuration.GetSection("AppSettings").GetValue<string>("PublicKeyFilename"),
                IPAddress = GetIPAddress(configuration.GetSection("AppSettings").GetValue<string>("IPAddress")),
                CultureName = configuration.GetSection("AppSettings").GetValue<string>("CultureName"),
                TreeFilename = configuration.GetSection("AppSettings").GetValue<string>("TreeFilename"),
                PathPattern = configuration.GetSection("AppSettings").GetValue<string>("PathPattern")
            };
        }

        private static SortedDictionary<int, int> GetStat(IServiceProvider serviceProvider)
        {
            return StatCollector.GetStat(serviceProvider.GetService<Tree>()!);
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

        public static void AddBotCommonServices(this IServiceCollection services)
        {
            services.AddSingleton(GetTree);
            services.AddSingleton(GetTelegramBotClient);
            services.AddSingleton(new ResourceManager("FiveLetters.Resources.Strings", typeof(BotApp).Assembly));
            services.AddSingleton(GetCultureInfo);
            services.AddSingleton<L10n>();
            services.AddSingleton(GetConfig);
            services.AddSingleton(GetStat);
        }
    }
}