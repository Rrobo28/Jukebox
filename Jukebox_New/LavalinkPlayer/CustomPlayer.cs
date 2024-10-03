using Discord.Interactions;
using Discord.Rest;
using Discord.WebSocket;
using Discord;
using Lavalink4NET.InactivityTracking.Players;
using Lavalink4NET.InactivityTracking.Trackers;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players;
using Lavalink4NET.Protocol.Payloads.Events;
using Lavalink4NET.Rest.Entities.Tracks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Formats.Asn1.AsnWriter;

public sealed class CustomPlayer : QueuedLavalinkPlayer, IInactivityPlayerListener, ILavalinkPlayerListener
{
    private readonly ISocketMessageChannel? _textChannel;

    public HashSet<FormatClass.Buttons> DissabledButtons = new HashSet<FormatClass.Buttons>();

    public RestUserMessage PlayerMessage = null;

    public IUserMessage LyricsMessage = null;

    public IUserMessage QueueMessage = null;

    public IUserMessage TrackSelectMessage = null;

    public Dictionary<IUser, Score> PlayersScore = new Dictionary<IUser, Score>();

    public TimeSpan SongStartSeconds;

    public SocketInteractionContext Context;

    public List<SelectMenuComponent> QueueMenus = new List<SelectMenuComponent>();

    public CustomPlayer(IPlayerProperties<CustomPlayer, CustomPlayerOptions> properties)
        : base(properties)
    {
        _textChannel = properties.Options.Value.TextChannel;
        Context = properties.Options.Value.context;
    }

    public new async ValueTask NotifyPlayerUpdateAsync(DateTimeOffset timestamp, TimeSpan position, bool connected, TimeSpan? latency, CancellationToken cancellationToken = default(CancellationToken))
    {
       
    }

    public async ValueTask NotifyPlayerActiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default(CancellationToken))
    {
    }

    public async ValueTask NotifyPlayerInactiveAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default(CancellationToken))
    {
        await FormatClass.Clean(this, _textChannel);
    }

    public async ValueTask NotifyPlayerTrackedAsync(PlayerTrackingState trackingState, CancellationToken cancellationToken = default(CancellationToken))
    {

    }

    public new async ValueTask DisconnectAsync(CancellationToken cancellationToken = default(CancellationToken))
    {
        await base.DisconnectAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        await FormatClass.Clean(this, _textChannel);
    }

    protected override async ValueTask NotifyTrackEndedAsync(ITrackQueueItem track, TrackEndReason endReason, CancellationToken cancellationToken = default(CancellationToken))
    {
        await base.NotifyTrackEndedAsync(track, endReason, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
       
    }

    protected override async ValueTask NotifyTrackStuckAsync(ITrackQueueItem track, TimeSpan threshold, CancellationToken cancellationToken = default(CancellationToken))
    {
        await base.NotifyTrackStuckAsync(track, threshold, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
    }

    protected override async ValueTask NotifyTrackExceptionAsync(ITrackQueueItem track, TrackException exception, CancellationToken cancellationToken = default(CancellationToken))
    {
        await base.NotifyTrackExceptionAsync(track, exception, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
    }

    protected override async ValueTask NotifyTrackStartedAsync(ITrackQueueItem track, CancellationToken cancellationToken = default(CancellationToken))
    {
        await base.NotifyTrackStartedAsync(track, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
        if (track == null)
        {
            await _textChannel.SendMessageAsync("TRACK IS NULL");
            return;
        }
        if (PlayerMessage != null)
        {
            await _textChannel.DeleteMessageAsync(PlayerMessage).ConfigureAwait(continueOnCapturedContext: false);
            PlayerMessage = null;
        }
        else
        {
            if (LyricsMessage != null)
            {
                await _textChannel.DeleteMessageAsync(LyricsMessage).ConfigureAwait(continueOnCapturedContext: false);
                LyricsMessage = null;
            }
            PlayerMessage = await _textChannel.SendMessageAsync("# NOW PLAYING  :arrow_forward:", isTTS: false, FormatClass.GetTrackEmbed(track.Track, this), null, null, null, FormatClass.GetMediaButtons(DissabledButtons)).ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    protected override async ValueTask NotifyTrackEnqueuedAsync(ITrackQueueItem queueItem, int position, CancellationToken cancellationToken = default(CancellationToken))
    {
        await base.NotifyTrackEnqueuedAsync(queueItem, position, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
    }
}
