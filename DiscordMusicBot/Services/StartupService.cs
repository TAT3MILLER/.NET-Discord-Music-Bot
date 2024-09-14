using Discord;
using Discord.WebSocket;
using DiscordMusicBot.Utils;
using Lavalink4NET;

namespace DiscordMusicBot.Services;

public class StartupService(
    DiscordSocketClient discord,
    IConfiguration config,
    AudioService audioService) : IHostedService
{
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await discord.LoginAsync(TokenType.Bot, config["Token"]);
        await discord.StartAsync();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await discord.LogoutAsync();
        await discord.StopAsync();
        await audioService.StopAsync(cancellationToken);
        Lavalink.Stop();
        discord.Dispose();
    }
}