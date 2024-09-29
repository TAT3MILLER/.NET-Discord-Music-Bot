using System.Collections.Immutable;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.Clients;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Preconditions;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Rest.Entities.Tracks;

namespace DiscordMusicBot.Modules;

[RequireContext(ContextType.Guild)]
public class AudioModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IAudioService _audioService;

    public AudioModule(IAudioService audioService)
    {
        ArgumentNullException.ThrowIfNull(audioService);
        _audioService = audioService;
    }

    
    // Would like to add some sort of resumption capability to this command.
    [SlashCommand("play", description: "Plays music", runMode: RunMode.Async)]
    public async Task Play(string query)
    {
        await DeferAsync().ConfigureAwait(false);

        var player = await GetPlayerAsync(allowConnect: true,
            preconditions: ImmutableArray.Create(PlayerPrecondition.Paused))
            .ConfigureAwait(false);

        if (player is null)
        {
            return;
        }

        var track = await _audioService.Tracks
            .LoadTrackAsync(query, TrackSearchMode.YouTube)
            .ConfigureAwait(false);

        if (track is null)
        {
            await FollowupAsync("😖 No results.").ConfigureAwait(false);
            return;
        }

        var position = await player.PlayAsync(track).ConfigureAwait(false);

        if (position is 0)
        {
            await FollowupAsync($"🔈 Playing: {track.Uri}").ConfigureAwait(false);
        }
        else
        {
            await FollowupAsync($"🔈 Added to queue: {track.Uri}").ConfigureAwait(false);
        }
    }
    
    [SlashCommand("stop", description: "Stops the current track", runMode: RunMode.Async)]
    public async Task Stop()
    {
        var player = await GetPlayerAsync(allowConnect: false, 
            preconditions: ImmutableArray.Create(PlayerPrecondition.Playing));

        if (player is null)
        {
            return;
        }

        if (player.CurrentItem is null)
        {
            await RespondAsync("Nothing playing!").ConfigureAwait(false);
            return;
        }

        await player.StopAsync().ConfigureAwait(false);
        await RespondAsync("Stopped playing.").ConfigureAwait(false);
    }
    
    [SlashCommand("skip", description: "Skips the current track", runMode: RunMode.Async)]
    public async Task Skip()
    {
        var player = await GetPlayerAsync(allowConnect: false, preconditions: ImmutableArray.Create(PlayerPrecondition.Playing, PlayerPrecondition.QueueNotEmpty));

        if (player is null)
        {
            return;
        }
        
        if (player.CurrentItem is null)
        {
            await RespondAsync("Nothing playing!").ConfigureAwait(false);
            return;
        }

        await player.SkipAsync().ConfigureAwait(false);

        var track = player.CurrentItem;

        if (track is not null)
        {
            await RespondAsync($"Skipped. Now playing: {track.Track!.Uri}").ConfigureAwait(false);
        }
        else
        {
            await RespondAsync("Skipped. Stopped playing because the queue is now empty.").ConfigureAwait(false);
        }
    }
    
    [SlashCommand("resume", description: "Resumes the player.", runMode: RunMode.Async)]
    public async Task ResumeAsync()
    {
        var player = await GetPlayerAsync(allowConnect: false, preconditions: ImmutableArray.Create(PlayerPrecondition.Paused));

        if (player is null)
        {
            return;
        }

        await player.ResumeAsync().ConfigureAwait(false);
        await RespondAsync("Resumed.").ConfigureAwait(false);
    }
    
    [SlashCommand("pause", description: "Pauses the player.", runMode: RunMode.Async)]
    public async Task PauseAsync()
    {
        var player = await GetPlayerAsync(allowConnect: false, preconditions: ImmutableArray.Create(PlayerPrecondition.NotPaused));

        if (player is null)
        {
            return;
        }

        if (player.State is PlayerState.Paused)
        {
            await RespondAsync("Player is already paused.").ConfigureAwait(false);
            return;
        }

        await player.PauseAsync().ConfigureAwait(false);
        await RespondAsync("Paused.").ConfigureAwait(false);
    }
    
    [SlashCommand("position", description: "Shows the track position", runMode: RunMode.Async)]
    public async Task Position()
    {
        var player = await GetPlayerAsync(allowConnect: false, preconditions: ImmutableArray.Create(PlayerPrecondition.Playing)).ConfigureAwait(false);

        if (player is null)
        {
            return;
        }

        if (player.CurrentItem is null)
        {
            await RespondAsync("Nothing playing!").ConfigureAwait(false);
            return;
        }

        await RespondAsync($"Position: {player.Position?.Position} / {player.CurrentTrack.Duration}.").ConfigureAwait(false);
    }

    [SlashCommand("queue", description: "Shows the current queue", runMode: RunMode.Async)]
    public async Task Queue()
    {
        var player = await GetPlayerAsync(allowConnect: false, preconditions: ImmutableArray.Create(PlayerPrecondition.QueueNotEmpty));

        if (player is null)
        {
            return;
        }

        if (player.Queue.IsEmpty)
        {
            await RespondAsync("Nothing in queue!").ConfigureAwait(false);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("Queued Tracks");
        
        var trackQueue = player.Queue;
        
        foreach (ITrackQueueItem t in trackQueue)
        {
            var title = t.Track.Title;
            var uri = t.Track.Uri;
            embed.AddField($"{title}", $"{uri.ToString()}");
        }
        
        await RespondAsync(embed: embed.Build());
    }

    [SlashCommand("shuffle", description: "Shuffles track queue", runMode: RunMode.Async)]
    public async Task Shuffle()
    {
        var player = await GetPlayerAsync(allowConnect: false, preconditions: ImmutableArray.Create(PlayerPrecondition.QueueNotEmpty)).ConfigureAwait(false);

        if (player is null)
        {
            return;
        }

        if (player.Queue.IsEmpty)
        {
            await RespondAsync("Nothing in queue!").ConfigureAwait(false);
            return;
        }

        try
        {
            await player.Queue.ShuffleAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        await RespondAsync("Tracks shuffled!");
    }
    
    [SlashCommand("volume", description: "Sets the player volume (0 - 1000%)", runMode: RunMode.Async)]
    public async Task Volume(int volume = 40)
    {
        if (volume is > 1000 or < 0)
        {
            await RespondAsync("Volume out of range: 0% - 1000%!").ConfigureAwait(false);
            return;
        }

        var player = await GetPlayerAsync(allowConnect: false).ConfigureAwait(false);

        if (player is null)
        {
            return;
        }

        await player.SetVolumeAsync(volume / 100f).ConfigureAwait(false);
        await RespondAsync($"Volume updated: {volume}%").ConfigureAwait(false);
    }
    
    [SlashCommand("disconnect", "Disconnects from the current voice channel connected to", runMode: RunMode.Async)]
    public async Task Disconnect()
    {
        var player = await GetPlayerAsync().ConfigureAwait(false);

        if (player is null)
        {
            return;
        }

        await player.DisconnectAsync().ConfigureAwait(false);
        await RespondAsync("Disconnected.").ConfigureAwait(false);
    }

    private async ValueTask<QueuedLavalinkPlayer?> GetPlayerAsync(
        bool allowConnect = false,
        bool requireChannel = true,
        ImmutableArray<IPlayerPrecondition> preconditions = default,
        bool isDeferred = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = new PlayerRetrieveOptions(
            ChannelBehavior: allowConnect ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None,
            VoiceStateBehavior: requireChannel ? MemberVoiceStateBehavior.RequireSame : MemberVoiceStateBehavior.Ignore,
            Preconditions: preconditions);

        var result = await _audioService.Players
            .RetrieveAsync(Context, playerFactory: PlayerFactory.Queued, options, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        if (result.IsSuccess)
        {
            return result.Player;
        }

        // See the error handling section for more information
        var errorMessage = CreateErrorEmbed(result);

        if (isDeferred)
        {
            await FollowupAsync(embed: errorMessage).ConfigureAwait(false);
        }
        else
        {
            await RespondAsync(embed: errorMessage).ConfigureAwait(false);
        }

        return null;
    }
    
    
    private static Embed CreateErrorEmbed(PlayerResult<QueuedLavalinkPlayer> result)
    {
        var title = result.Status switch
        {
            PlayerRetrieveStatus.UserNotInVoiceChannel => "You must be in a voice channel.",
            PlayerRetrieveStatus.BotNotConnected => "The bot is not connected to any channel.",
            PlayerRetrieveStatus.VoiceChannelMismatch => "You must be in the same voice channel as the bot.",

            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.Playing => "The player is currently now playing any track.",
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.NotPaused => "The player is already paused.",
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.Paused => "The player is not paused.",
            PlayerRetrieveStatus.PreconditionFailed when result.Precondition == PlayerPrecondition.QueueEmpty => "The queue is empty.",

            _ => "Unknown error.",
        };

        return new EmbedBuilder().WithTitle(title).Build();
    }
}