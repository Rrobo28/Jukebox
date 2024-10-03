using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

internal sealed class DiscordClientHost : IHostedService
{
    private readonly DiscordSocketClient _discordSocketClient;

    private readonly InteractionService _interactionService;

    private readonly CommandService commands;

    private readonly IServiceProvider _serviceProvider;

    private MusicModule musicModule;

    public DiscordClientHost(DiscordSocketClient discordSocketClient, InteractionService interactionService, CommandService _commands, IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(discordSocketClient);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(_commands);

        _discordSocketClient = discordSocketClient;
        _interactionService = interactionService;
        _serviceProvider = serviceProvider;
        commands = _commands;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _discordSocketClient.Ready += ClientReady;
        _discordSocketClient.InteractionCreated += InteractionCreated;
        
        _discordSocketClient.MessageReceived += HandleCommandAsync;
        await _discordSocketClient.LoginAsync(TokenType.Bot, File.ReadAllText("Token.txt")).ConfigureAwait(continueOnCapturedContext: false);
        await _discordSocketClient.StartAsync().ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _discordSocketClient.Ready -= ClientReady;
        _discordSocketClient.InteractionCreated -= InteractionCreated;
        _discordSocketClient.MessageReceived -= HandleCommandAsync;

        await _discordSocketClient.StopAsync().ConfigureAwait(false);
    }

    private Task InteractionCreated(SocketInteraction interaction)
    {
        SocketInteractionContext interactionContext = new SocketInteractionContext(_discordSocketClient, interaction);
        return _interactionService.ExecuteCommandAsync(interactionContext, _serviceProvider);
    }

    private async Task ClientReady()
    {
        try
        {
            await _interactionService.AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider).ConfigureAwait(false);
            await _interactionService.RegisterCommandsGloballyAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error during ClientReady: {ex.Message}");
        }
    }

    private async Task HandleCommandAsync(SocketMessage MessageParam)
    {
        try
        {
            if (MessageParam is SocketUserMessage message)
            {
                int argPos = 0;
                if (!message.Author.IsBot && message.HasStringPrefix("!", ref argPos)) // Assuming '!' is your command prefix
                {
                    SocketCommandContext context = new SocketCommandContext(_discordSocketClient, message);
                    await commands.ExecuteAsync(context, argPos, null);
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error
            Console.WriteLine($"Error handling command: {ex.Message}");
        }
    }
}

