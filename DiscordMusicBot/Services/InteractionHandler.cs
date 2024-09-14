using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using IResult = Discord.Interactions.IResult;

namespace DiscordMusicBot.Services;

public class InteractionHandler(
    DiscordSocketClient discord,
    CommandService command,
    InteractionService handler,
    IServiceProvider services,
    ILogger<InteractionHandler> logger)
{
    public async Task InitializeAsync()
    {
        discord.Ready += ReadyAsync;
        discord.Log += LogAsync;
        handler.Log += LogAsync;
        command.Log += LogAsync;
        
        await handler.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        
        discord.InteractionCreated += HandleInteraction;
        handler.InteractionExecuted += HandleInteractionExecute;
        
    }

    private async Task ReadyAsync()
    {
        //Use this if you want the bot to register commands across every server
        //await handler.RegisterCommandsGloballyAsync(true);
        
        // Use this if you want commands to just be registered to your server
        await handler
            .RegisterCommandsToGuildAsync(649125115287830547)
            .ConfigureAwait(false);
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
        try
        {
            var context = new SocketInteractionContext(discord, interaction);
            
            var result = await handler.ExecuteCommandAsync(context, services);
            
            if (!result.IsSuccess)
                
                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        logger.LogWarning(result.ErrorReason);
                        break;
                    default:
                        break;
                }
        }
        catch
        {
            if (interaction.Type is InteractionType.ApplicationCommand)
                await interaction.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
        }
    }

    private Task HandleInteractionExecute(ICommandInfo commandInfo, IInteractionContext context, IResult result)
    {
        if (!result.IsSuccess)
            switch (result.Error)
            {
                case InteractionCommandError.UnmetPrecondition:
                    logger.LogWarning(result.ErrorReason);
                    break;
                default:
                    break;
            }

        return Task.CompletedTask;
    }

    private Task LogAsync(LogMessage msg)
    {
        if (msg.Exception is CommandException commandException)
        {
            Console.WriteLine($"[Command/{msg.Severity}] {commandException.Command.Aliases.First()}"
                              + $" failed to execute in {commandException.Context.Channel}.");
            logger.LogInformation($"[Command/{msg.Severity}] {commandException.Command.Aliases.First()}"
                                   + $" failed to execute in {commandException.Context.Channel}.");
            Console.WriteLine(commandException);
            logger.LogInformation(commandException.ToString());
        }
        else if (msg.Exception is InteractionException interactionException)
        {
            Console.WriteLine($"[Interaction/{msg.Severity}] {interactionException}");
            logger.LogInformation($"[Interaction/{msg.Severity}] {interactionException}");
        }
        else
        {
            Console.WriteLine($"[General/{msg.Severity}] {msg.ToString()}");
            logger.LogInformation($"[General/{msg.Severity}] {msg.ToString()}");
        }
        return Task.CompletedTask;
    }
}