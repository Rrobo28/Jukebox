using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Lavalink4NET.Lyrics;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Lavalink4NET;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lavalink4NET.DiscordNet;
using System.Net.Http.Headers;
using System.Collections;

[RequireContext(ContextType.Guild)]
public sealed class MusicModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IAudioService _audioService;

    private readonly ILyricsService _lyricsService;

    //Constructor 
    public MusicModule(IAudioService audioService, ILyricsService lyricsService)
    {
        _audioService = audioService;
        _lyricsService = lyricsService;
    }

    public enum dndCatagories { Calm, Dark, Night, Day, Combat, Villin, City, Tavern };

    [SlashCommand("dnd", "Ambient Music For DND", false, RunMode.Async)]
    public async Task DND(dndCatagories catagory)
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);

        CustomPlayer player = await GetPlayerAsync().ConfigureAwait(continueOnCapturedContext: false);

        await player.Queue.ClearAsync();
        await player.RefreshAsync();
        player.Shuffle = true;

        List<string> playlists = new List<string>
        {
            "https://open.spotify.com/playlist/3P3mMrJd1UTHykwCGpd3A5?si=da19aaf3fc3e4406",
            "https://open.spotify.com/playlist/3R3dzuLnwYtdALbU4PFsIS?si=982f993a4e754537",
            "https://open.spotify.com/playlist/3P3mMrJd1UTHykwCGpd3A5?si=6d06ad8dd9b34bc3",
            "https://open.spotify.com/playlist/3P3mMrJd1UTHykwCGpd3A5?si=6d06ad8dd9b34bc3",
            "https://open.spotify.com/playlist/3D54wKu7Knqu9bbfWS0LOD?si=2a942e0e1a6f44cd",
            "https://open.spotify.com/playlist/5CsvEHl4WNiSAVSY6T0myA?si=4e4b50603dc84495",
            "https://open.spotify.com/playlist/4lkKBikGar09W5Hvsgb4Mc?si=e2d84fae38034fff",
            "https://open.spotify.com/playlist/6SN4l8Oxu4HhEGYr74CDNJ?si=ca0097faf4444ab0"
        };



        string LinkChosen = playlists[((int)catagory)];

        var searchMode = TrackSearchMode.Spotify;
        TrackLoadResult tracks = await _audioService.Tracks.LoadTracksAsync(LinkChosen, searchMode).ConfigureAwait(continueOnCapturedContext: false);

        if (!tracks.IsSuccess)
        {
            await FormatClass.DeleteMessage(await ReplyAsync("\ud83d\ude16 No results.").ConfigureAwait(continueOnCapturedContext: false), base.Context, 1000);
        }
        else
        {
            ImmutableArray<LavalinkTrack>.Enumerator enumerator = tracks.Tracks.GetEnumerator();
            while (enumerator.MoveNext())
            {
                LavalinkTrack track = enumerator.Current;
                await player.PauseAsync();
                await player.PlayAsync(track).ConfigureAwait(continueOnCapturedContext: false);
            }
            //FormatClass.DeleteMessage(await FollowupAsync("# ADDED TO QUEUE :arrows_clockwise: ", null, isTTS: false, ephemeral: false, null, null, null, FormatClass.GetPlaylistEmbed(tracks.Playlist)).ConfigureAwait(continueOnCapturedContext: false), base.Context, 5000);
        }

        await player.SkipAsync();
        await player.ResumeAsync();
    }



    [ComponentInteraction("Disconnect", false, RunMode.Default)]
    public async Task Disconnect()
    {
        CustomPlayer player = await GetPlayerAsync().ConfigureAwait(continueOnCapturedContext: false);
        if (player != null)
        {
            IUserMessage message = await ReplyAsync("# Disconnected").ConfigureAwait(continueOnCapturedContext: false);
            await player.DisconnectAsync().ConfigureAwait(continueOnCapturedContext: false);
            await FormatClass.DeleteMessage(message, base.Context, 1000);
        }
    }


    [SlashCommand("play", "Plays music", false, RunMode.Async)]
    public async Task Play(string query)
    {
        //Acnowlage the Command 
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);


        //Retrive the player
        CustomPlayer player = await GetPlayerAsync().ConfigureAwait(continueOnCapturedContext: false);

        //Defualt search mode is Youtube for querys 
        TrackSearchMode searchMode = TrackSearchMode.YouTube;

        //Check to see if a link was posted 
       if (query.StartsWith("https://open.spotify.com/"))
        {
            searchMode = TrackSearchMode.Spotify;
        }
       else if (query.StartsWith("https://youtu.be/") || query.StartsWith("https://www.youtube.com/watch?"))
        {
            searchMode = TrackSearchMode.YouTube;
        }
      
        //All the Tracks in the result 
        TrackLoadResult tracks = ((!(searchMode != TrackSearchMode.None)) ? (await _audioService.Tracks.LoadTracksAsync(query, TrackSearchMode.YouTube).ConfigureAwait(continueOnCapturedContext: false)) : (await _audioService.Tracks.LoadTracksAsync(query, searchMode).ConfigureAwait(continueOnCapturedContext: false)));
        
        if (!tracks.IsSuccess)
        {
            //Send a message that is deleted after 10 seconds
            await FormatClass.DeleteMessage(await ReplyAsync("\ud83d\ude16 No results.").ConfigureAwait(continueOnCapturedContext: false), base.Context, 1000);
        }
        else
        {
            if (player == null)
            {
                return;
            }
            if (!tracks.IsPlaylist)
            {
                //Plays the track or adds it to queue
                await player.PlayAsync(tracks.Track).ConfigureAwait(continueOnCapturedContext: false);
                if (!player.Queue.IsEmpty)
                {
                    //Show a quick message of the Track that has been added 
                    await FormatClass.DeleteMessage(await FollowupAsync("# ADDED TO QUEUE :arrows_clockwise: ", null, isTTS: false, ephemeral: false, null, null, null, FormatClass.GetTrackEmbed(tracks.Track, player)).ConfigureAwait(continueOnCapturedContext: false), base.Context, 1000);
                }
            }
            else
            {
                //Loops through the playlist, adds all the tracks to the bot then shows a quick message of the playlist that has been added.
                ImmutableArray<LavalinkTrack>.Enumerator enumerator = tracks.Tracks.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    LavalinkTrack track = enumerator.Current;
                    await player.PlayAsync(track).ConfigureAwait(continueOnCapturedContext: false);
                }
                await FormatClass.DeleteMessage(await FollowupAsync("# ADDED TO QUEUE :arrows_clockwise: ", null, isTTS: false, ephemeral: false, null, null, null, FormatClass.GetPlaylistEmbed(tracks.Playlist)).ConfigureAwait(continueOnCapturedContext: false), base.Context, 5000);
            }
          
        }
    }

    [ComponentInteraction("Lyrics", false, RunMode.Default)]
    public async Task Lyrics()
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        if (player == null)
        {
            await FormatClass.DeleteMessage(await ReplyAsync("# No player ").ConfigureAwait(continueOnCapturedContext: false), base.Context, 1000);
            return;
        }
        LavalinkTrack track = player.CurrentTrack;
        if (player.LyricsMessage != null)
        {
            IUserMessage message = player.LyricsMessage;
            await FormatClass.DeleteMessage(message, base.Context);
            player.LyricsMessage = null;
            return;
        }
        if ((object)track == null)
        {
            await FormatClass.DeleteMessage(await ReplyAsync("# \ud83e\udd14 No track is currently playing.").ConfigureAwait(continueOnCapturedContext: false), base.Context, 1000);
            return;
        }
        string lyrics = await _lyricsService.GetLyricsAsync(track.Author, track.Title).ConfigureAwait(continueOnCapturedContext: false);
        if (lyrics == null)
        {
            await FormatClass.DeleteMessage(await ReplyAsync("# \ud83d\ude16 No lyrics found.").ConfigureAwait(continueOnCapturedContext: false), base.Context, 1000);
            return;
        }
        lyrics = string.Join(value: Regex.Split(lyrics, "\r\n|\r|\n").Skip(1).ToArray(), separator: Environment.NewLine);
        CustomPlayer customPlayer = player;
        customPlayer.LyricsMessage = await FollowupAsync($"# \ud83d\udcc3 Lyrics for {track.Title} by {track.Author}:\n```{lyrics}```").ConfigureAwait(continueOnCapturedContext: false);
    }

    [ComponentInteraction("Repeat", false, RunMode.Default)]
    public async Task Repeat()
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        if (player != null)
        {
            if (player.RepeatMode == TrackRepeatMode.Track)
            {
                player.RepeatMode = TrackRepeatMode.None;
                player.DissabledButtons.Remove(FormatClass.Buttons.Repeat);
            }
            else
            {
                player.RepeatMode = TrackRepeatMode.Track;
                player.DissabledButtons.Add(FormatClass.Buttons.Repeat);
            }
            await base.Context.Channel.DeleteMessageAsync(player.PlayerMessage);
            CustomPlayer customPlayer = player;
            customPlayer.PlayerMessage = await base.Context.Channel.SendMessageAsync("# NOW PLAYING  :arrow_forward:", isTTS: false, FormatClass.GetTrackEmbed(player.CurrentTrack, player), null, null, null, FormatClass.GetMediaButtons(player.DissabledButtons));
        }
    }

    [ComponentInteraction("Shuffle", false, RunMode.Default)]
    public async Task Shuffle()
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        if (player != null)
        {
            if (player.Shuffle)
            {
                player.Shuffle = false;
                player.DissabledButtons.Remove(FormatClass.Buttons.Shuffle);
            }
            else
            {
                player.Shuffle = true;
                player.DissabledButtons.Add(FormatClass.Buttons.Shuffle);
            }
            await base.Context.Channel.DeleteMessageAsync(player.PlayerMessage);
            CustomPlayer customPlayer = player;
            customPlayer.PlayerMessage = await base.Context.Channel.SendMessageAsync("# NOW PLAYING  :arrow_forward:", isTTS: false, FormatClass.GetTrackEmbed(player.CurrentTrack, player), null, null, null, FormatClass.GetMediaButtons(player.DissabledButtons));
        }
    }

    [ComponentInteraction("Selection", false, RunMode.Default)]
    public async Task Select()
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
    }

    [ComponentInteraction("Queue", false, RunMode.Default)]
    public async Task Queue()
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        if (player == null)
        {
            return;
        }
        if (player.Queue == null || player.Queue.IsEmpty)
        {
            await FormatClass.DeleteMessage(await ReplyAsync("# QUEUE \ud83d\udd0e\n ## Nothing Queued").ConfigureAwait(continueOnCapturedContext: false), base.Context, 1000);
            return;
        }
        player.QueueMenus.Clear();
        SelectMenuBuilder menu = new SelectMenuBuilder();
        menu.WithPlaceholder("View The Queue");
        menu.WithCustomId("Track_Selection_" + 0);
        string QueueString = "# QUEUE \ud83d\udd0e  \n";
        ComponentBuilder builder = new ComponentBuilder();
        int counter = 0;
        for (int i = 0; i < player.Queue.Count; i++)
        {
            menu.AddOption(player.Queue[i].Track.Title.ToString(), i.ToString(), player.Queue[i].Track.Author.ToString());
            counter++;
            if (counter == 25)
            {
                player.QueueMenus.Add(menu.Build());
                menu = new SelectMenuBuilder();
                menu.WithPlaceholder("View The Queue");
                menu.WithCustomId("Track_Selection_" + i + 1);
                counter = 0;
            }
        }
        player.QueueMenus.Add(menu.Build());
        builder.WithSelectMenu(player.QueueMenus[0].ToBuilder());
        if (player.QueueMenus.Count > 1)
        {
            builder.WithButton(" ", "Previous_Menu", ButtonStyle.Success, new Emoji("⬅"));
            builder.WithButton(" ", "Next_Menu", ButtonStyle.Success, new Emoji("➡"));
        }
        builder.WithButton(" ", "Cancel_Queue_Menu", ButtonStyle.Danger, new Emoji("✖"), null, disabled: false, 1);
        CustomPlayer customPlayer = player;
        customPlayer.QueueMessage = await FollowupAsync(QueueString, null, isTTS: false, ephemeral: false, null, null, builder.Build()).ConfigureAwait(continueOnCapturedContext: false);
    }

    [ComponentInteraction("Next_Menu", false, RunMode.Default)]
    public async Task NextQueueMenu()
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
        SocketMessageComponent component = (SocketMessageComponent)base.Context.Interaction;
        IMessage originalMessage = await base.Context.Channel.GetMessageAsync(component.Message.Id);
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        if (player == null)
        {
            return;
        }
        SelectMenuComponent selectMenuComponent = null;
        foreach (IMessageComponent menu in originalMessage.Components)
        {
            if (!(menu is ActionRowComponent actionRow))
            {
                continue;
            }
            foreach (IMessageComponent innerComponent in actionRow.Components)
            {
                if (innerComponent is SelectMenuComponent)
                {
                    selectMenuComponent = (SelectMenuComponent)innerComponent;
                    break;
                }
            }
        }
        int currentMenu = -1;
        for (int i = 0; i < player.QueueMenus.Count; i++)
        {
            if (player.QueueMenus[i].CustomId == selectMenuComponent.CustomId)
            {
                currentMenu = i;
                break;
            }
        }
        if (currentMenu != -1)
        {
            ComponentBuilder builder = new ComponentBuilder();
            if (currentMenu + 1 > player.QueueMenus.Count)
            {
                builder.WithSelectMenu(player.QueueMenus[0].ToBuilder());
            }
            else
            {
                builder.WithSelectMenu(player.QueueMenus[currentMenu + 1].ToBuilder());
            }
            builder.WithButton(" ", "Previous_Menu", ButtonStyle.Success, new Emoji("⬅"));
            builder.WithButton(" ", "Next_Menu", ButtonStyle.Success, new Emoji("➡"));
            builder.WithButton(" ", "Cancel_Queue_Menu", ButtonStyle.Danger, new Emoji("✖"), null, disabled: false, 1);
            await base.Context.Channel.ModifyMessageAsync(component.Message.Id, delegate (MessageProperties msg)
            {
                msg.Components = builder.Build();
            });
        }
    }

    [ComponentInteraction("Previous_Menu", false, RunMode.Default)]
    public async Task PevQueueMenu()
    {
        SocketMessageComponent component = (SocketMessageComponent)base.Context.Interaction;
        IMessage originalMessage = await base.Context.Channel.GetMessageAsync(component.Message.Id);
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        if (player == null)
        {
            return;
        }
        SelectMenuComponent selectMenuComponent = null;
        foreach (IMessageComponent menu in originalMessage.Components)
        {
            if (!(menu is ActionRowComponent actionRow))
            {
                continue;
            }
            foreach (IMessageComponent innerComponent in actionRow.Components)
            {
                if (innerComponent is SelectMenuComponent)
                {
                    selectMenuComponent = (SelectMenuComponent)innerComponent;
                    break;
                }
            }
        }
        int currentMenu = -1;
        for (int i = 0; i < player.QueueMenus.Count; i++)
        {
            if (player.QueueMenus[i].CustomId == selectMenuComponent.CustomId)
            {
                currentMenu = i;
                break;
            }
        }
        if (currentMenu != -1)
        {
            ComponentBuilder builder = new ComponentBuilder();
            if (currentMenu - 1 < 0)
            {
                builder.WithSelectMenu(player.QueueMenus[player.QueueMenus.Count].ToBuilder());
            }
            else
            {
                builder.WithSelectMenu(player.QueueMenus[currentMenu - 1].ToBuilder());
            }
            builder.WithButton(" ", "Previous_Menu", ButtonStyle.Success, new Emoji("⬅"));
            builder.WithButton(" ", "Next_Menu", ButtonStyle.Success, new Emoji("➡"));
            builder.WithButton(" ", "Cancel_Queue_Menu", ButtonStyle.Danger, new Emoji("✖"), null, disabled: false, 1);
            await base.Context.Channel.ModifyMessageAsync(component.Message.Id, delegate (MessageProperties msg)
            {
                msg.Components = builder.Build();
            });
        }
    }

    [ComponentInteraction("Cancel_Queue_Menu", false, RunMode.Default)]
    public async Task CancelQueueInteraction()
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        if (player != null && player.QueueMessage != null)
        {
            await base.Context.Channel.DeleteMessageAsync(player.QueueMessage);
        }
    }

    [ComponentInteraction("Track_Selection_*", false, RunMode.Default)]
    public async Task SelectTrack()
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        SocketMessageComponent component = (SocketMessageComponent)base.Context.Interaction;
        int id = int.Parse(component.Data.Values.ElementAtOrDefault(0));
        if (player != null)
        {
            LavalinkTrack SelectedTrack = player.Queue[id].Track;
            if (!(SelectedTrack == null))
            {
                await base.Context.Channel.DeleteMessageAsync(player.QueueMessage);
                ComponentBuilder componentBuilder = new ComponentBuilder();
                componentBuilder.WithButton(" ", "BumpTrack" + id, ButtonStyle.Primary, new Emoji("⏫"));
                componentBuilder.WithButton(" ", "MoveTrack" + id, ButtonStyle.Primary, new Emoji("\ud83d\udd22"));
                componentBuilder.WithButton(" ", "DeleteTrack" + id, ButtonStyle.Danger, new Emoji("✖"));
                player.TrackSelectMessage = await FollowupAsync("# Your Selection", null, isTTS: false, ephemeral: false, null, null, embed: FormatClass.GetQueueEmbed(SelectedTrack), components: componentBuilder.Build());
            }
        }
    }

    [ComponentInteraction("BumpTrack*", false, RunMode.Default)]
    public async Task BumpTrack(string id)
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
        int selectedTrackID = int.Parse(id);
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        if (player != null)
        {
            ITrackQueueItem track = player.Queue[selectedTrackID];
            await player.Queue.RemoveAsync(track);
            await player.Queue.InsertAsync(0, track);
            await base.Context.Channel.DeleteMessageAsync(player.TrackSelectMessage);
            await FormatClass.DeleteMessage(await base.Context.Channel.SendMessageAsync("# Bumped Track"), base.Context, 1000);
        }
    }

    [ComponentInteraction("DeleteTrack*", false, RunMode.Default)]
    public async Task DeleteFromQueue(string id)
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
        int selectedTrackID = int.Parse(id);
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        if (player != null)
        {
            ITrackQueueItem track = player.Queue[selectedTrackID];
            await player.Queue.RemoveAsync(track);
            await base.Context.Channel.DeleteMessageAsync(player.TrackSelectMessage);
            await FormatClass.DeleteMessage(await base.Context.Channel.SendMessageAsync("# Track Removed"), base.Context, 1000);
        }
    }

    public async Task Position()
    {
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        if (player != null)
        {
            if (player.CurrentItem == null)
            {
                await FormatClass.DeleteMessage(await ReplyAsync("Nothing playing!").ConfigureAwait(continueOnCapturedContext: false), base.Context, 1000);
                return;
            }
            await RespondAsync($"Position: {player.Position?.Position} / {player.CurrentTrack.Duration}.").ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    public async Task Stop()
    {
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false);
        if (player != null)
        {
            if (player.CurrentItem == null)
            {
                await ReplyAsync("Nothing playing!").ConfigureAwait(continueOnCapturedContext: false);
                return;
            }
            await player.StopAsync().ConfigureAwait(continueOnCapturedContext: false);
            await RespondAsync("Stopped playing.").ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    public async Task Volume(int volume = 50)
    {
        if ((volume > 1000 || volume < 0) ? true : false)
        {
            await RespondAsync("Volume out of range: 0% - 1000%!").ConfigureAwait(continueOnCapturedContext: false);
            return;
        }
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false).ConfigureAwait(continueOnCapturedContext: false);
        if (player != null)
        {
            await player.SetVolumeAsync((float)volume / 100f).ConfigureAwait(continueOnCapturedContext: false);
            await RespondAsync($"Volume updated: {volume}%").ConfigureAwait(continueOnCapturedContext: false);
        }
    }

    [ComponentInteraction("Skip", false, RunMode.Default)]
    public async Task Skip()
    {
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false);
        if (player == null)
        {
            return;
        }
        if (player.CurrentItem == null)
        {
            await FormatClass.DeleteMessage(await ReplyAsync("Nothing playing!").ConfigureAwait(continueOnCapturedContext: false), base.Context, 1000);
            return;
        }
        await player.SkipAsync().ConfigureAwait(continueOnCapturedContext: false);
        ITrackQueueItem track = player.CurrentItem;
        if (track == null)
        {
            await FormatClass.DeleteMessage(await ReplyAsync("Skipped. Stopped playing because the queue is now empty.").ConfigureAwait(continueOnCapturedContext: false), base.Context, 1000);
        }
    }

    [ComponentInteraction("Pause", false, RunMode.Default)]
    public async Task PauseAsync()
    {
        await DeferAsync().ConfigureAwait(continueOnCapturedContext: false);
        CustomPlayer player = await GetPlayerAsync(connectToVoiceChannel: false);
        if (player != null)
        {
            if (player.State == PlayerState.Paused)
            {
                await player.ResumeAsync().ConfigureAwait(continueOnCapturedContext: false);
                player.DissabledButtons.Remove(FormatClass.Buttons.Play);
            }
            else
            {
                player.DissabledButtons.Add(FormatClass.Buttons.Play);
                await player.PauseAsync().ConfigureAwait(continueOnCapturedContext: false);
            }
            await base.Context.Channel.DeleteMessageAsync(player.PlayerMessage);
            CustomPlayer customPlayer = player;
            customPlayer.PlayerMessage = await base.Context.Channel.SendMessageAsync("# NOW PLAYING  :arrow_forward:", isTTS: false, FormatClass.GetTrackEmbed(player.CurrentTrack, player), null, null, null, FormatClass.GetMediaButtons(player.DissabledButtons));
        }
    }

    private async ValueTask<CustomPlayer?> GetPlayerAsync(bool connectToVoiceChannel = true)
    {
        PlayerRetrieveOptions retrieveOptions = new PlayerRetrieveOptions(connectToVoiceChannel ? PlayerChannelBehavior.Join : PlayerChannelBehavior.None);
        ISocketMessageChannel textChannel = base.Context.Interaction.Channel;
        PlayerResult<CustomPlayer> result = await PlayerManagerExtensions.RetrieveAsync<CustomPlayer, CustomPlayerOptions>(options: Options.Create(new CustomPlayerOptions(textChannel, base.Context)), playerManager: _audioService.Players, interactionContext: base.Context, playerFactory: CreatePlayer, retrieveOptions: retrieveOptions).ConfigureAwait(continueOnCapturedContext: false);
        if (!result.IsSuccess)
        {
            PlayerRetrieveStatus status = result.Status;
            if (1 == 0)
            {
            }
            string text = status switch
            {
                PlayerRetrieveStatus.UserNotInVoiceChannel => "You are not connected to a voice channel.",
                PlayerRetrieveStatus.BotNotConnected => "The bot is currently not connected.",
                _ => "Unknown error.",
            };
            if (1 == 0)
            {
            }
            string errorMessage = text;
            await FollowupAsync(errorMessage).ConfigureAwait(continueOnCapturedContext: false);
            return null;
        }
        return result.Player;
        static ValueTask<CustomPlayer> CreatePlayer(IPlayerProperties<CustomPlayer, CustomPlayerOptions> properties, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(new CustomPlayer(properties));
        }
    }
}
