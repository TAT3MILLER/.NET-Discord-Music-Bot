using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordMusicBot;
using DiscordMusicBot.Services;
using DiscordMusicBot.Utils;
using Lavalink4NET;
using Lavalink4NET.Extensions;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile("appsettings.json", false);
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.AddSingleton(x => new DiscordSocketClient(new DiscordSocketConfig()
        {
            GatewayIntents = Discord.GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
            LogGatewayIntentWarnings = true,
            AlwaysDownloadUsers = true,
            LogLevel = LogSeverity.Info
        }));
        services.AddSingleton<InteractionService>();
        services.AddSingleton<InteractionHandler>();
        services.AddSingleton<CommandService>();
        services.AddLogging(hostContext => hostContext.AddConsole());
        services.AddSingleton<AudioService>();
        services.AddLavalink();
        services.ConfigureLavalink(audioConfig =>
        {
            audioConfig.ResumptionOptions = new LavalinkSessionResumptionOptions(TimeSpan.FromSeconds(60));
            var settings = hostContext.Configuration.GetSection("LavalinkSettings").Get<LavalinkSettings>();
            audioConfig.Passphrase = settings.passphrase;

        });
        services.AddHostedService<StartupService>();
    })
    .Build();

Lavalink.Start();

var audioService = host.Services.GetRequiredService<IAudioService>();
await host.Services.GetRequiredService<InteractionHandler>().InitializeAsync();
    
await host.RunAsync();