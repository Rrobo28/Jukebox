using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Players.Queued;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public sealed record CustomPlayerOptions(ISocketMessageChannel? TextChannel, SocketInteractionContext context) : QueuedLavalinkPlayerOptions();
