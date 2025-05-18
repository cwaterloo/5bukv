using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;

namespace FiveLetters
{
    public sealed class BotWebHookApp(TelegramBotClient client, Config config, L10n l10n) : BackgroundService
    {
        public static async Task Run(string[] args)
        {
            await Host.CreateDefaultBuilder(args).ConfigureServices(services =>
            {
                services.AddHostedService<BotWebHookApp>();
                services.AddBotCommonServices();
                
            }).Build().RunAsync();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await client.DeleteMyCommandsAsync(cancellationToken: stoppingToken);
            await client.SetMyCommandsAsync([new BotCommand("/start", l10n.GetResourceString("StartDescription")),
                new BotCommand("/help", l10n.GetResourceString("HelpDescription"))], cancellationToken: stoppingToken);
            await client.DeleteWebhookAsync(cancellationToken: stoppingToken);
            using FileStream fileStream = new(config.PublicKeyFilename!, FileMode.Open, FileAccess.Read, FileShare.Read);
            using StreamContent streamContent = new(fileStream);
            await client.SetWebhookAsync(config.WebHookUrl!.ToString(), certificate: new(streamContent, "certificate.key"),
                secretToken: config.SecretToken!, cancellationToken: stoppingToken, dropPendingUpdates: true,
                ipAddress: config.IPAddress!.ToString());
        }
    }
}