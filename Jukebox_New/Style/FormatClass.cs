using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using static System.Formats.Asn1.AsnWriter;

public static class FormatClass
{
    public enum Buttons
    {
        None,
        Repeat,
        Shuffle,
        Queue,
        Play,
        Skip,
        Disconnect,
        Lyrics
    }

    public static Embed GetPlaylistEmbed(PlaylistInformation Playlist)
    {
        EmbedBuilder embedBuilder = new EmbedBuilder();
        if (Playlist.Name != null)
        {
            embedBuilder.WithTitle(Playlist.Name);
        }
        if (Playlist.AdditionalInformation.Count > 0)
        {
            embedBuilder.WithAuthor(Playlist.AdditionalInformation["author"].ToString());
            embedBuilder.WithUrl(Playlist.AdditionalInformation["url"].ToString());
            embedBuilder.WithThumbnailUrl(Playlist.AdditionalInformation["artworkUrl"].ToString());
            string sourceName = "";
            sourceName = Playlist.AdditionalInformation["type"].ToString();
            sourceName = char.ToUpper(sourceName[0]) + sourceName.Substring(1);
            embedBuilder.WithFooter(sourceName);
            EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();
            fieldBuilder.WithName("No. Tracks").WithValue(Playlist.AdditionalInformation["totalTracks"].ToString());
            fieldBuilder.WithIsInline(isInline: true);
            embedBuilder.AddField(fieldBuilder);
        }
        return embedBuilder.Build();
    }

    public static MessageComponent GetQueueButtons(LavalinkTrack track, CustomPlayer player)
    {
        ComponentBuilder buttonBuilder = new ComponentBuilder();
        buttonBuilder.WithButton(" ", "Remove" + track.Identifier, ButtonStyle.Danger, new Emoji("✖"));
        return buttonBuilder.Build();
    }

    public static Embed GetQueueEmbed(LavalinkTrack track)
    {
        EmbedBuilder embedBuilder = new EmbedBuilder();
        embedBuilder.WithColor(Color.Orange);
        if (track.Title != null)
        {
            embedBuilder.WithTitle(track.Title);
        }
        if ((object)track.Uri != null)
        {
            embedBuilder.WithUrl(track.Uri.ToString());
        }
        if (track.Author != null)
        {
            embedBuilder.WithAuthor(track.Author.ToString());
        }
        if ((object)track.ArtworkUri != null)
        {
            embedBuilder.WithThumbnailUrl(track.ArtworkUri.ToString());
        }
        return embedBuilder.Build();
    }

    public static MessageComponent GetMediaButtons(HashSet<Buttons> buttons)
    {
        ComponentBuilder builder = new ComponentBuilder();
        ButtonStyle RepeatStyle = (buttons.Contains(Buttons.Repeat) ? ButtonStyle.Success : ButtonStyle.Secondary);
        ButtonStyle ShuffleStyle = (buttons.Contains(Buttons.Shuffle) ? ButtonStyle.Success : ButtonStyle.Secondary);
        ButtonStyle QueueStyle = (buttons.Contains(Buttons.Queue) ? ButtonStyle.Secondary : ButtonStyle.Secondary);
        ButtonStyle LyricsStyle = (buttons.Contains(Buttons.Lyrics) ? ButtonStyle.Secondary : ButtonStyle.Secondary);
        ButtonStyle PauseStyle = (buttons.Contains(Buttons.Play) ? ButtonStyle.Danger : ButtonStyle.Success);
        ButtonStyle SkipStyle = (buttons.Contains(Buttons.Skip) ? ButtonStyle.Success : ButtonStyle.Success);
        ButtonStyle DisconnectStyle = (buttons.Contains(Buttons.Disconnect) ? ButtonStyle.Secondary : ButtonStyle.Danger);
        builder.WithButton(" ", "Pause", PauseStyle, new Emoji("⏯"));
        builder.WithButton(" ", "Skip", SkipStyle, new Emoji("⏭"));
        builder.WithButton(" ", "Repeat", RepeatStyle, new Emoji("\ud83d\udd04"), null, disabled: false, 1);
        builder.WithButton(" ", "Shuffle", ShuffleStyle, new Emoji("\ud83d\udd00"), null, disabled: false, 1);
        builder.WithButton(" ", "Queue", QueueStyle, new Emoji("\ud83d\udd0e"), null, disabled: false, 2);
        builder.WithButton(" ", "Lyrics", LyricsStyle, new Emoji("\ud83c\udf99"), null, disabled: false, 2);
        builder.WithButton(" ", "Disconnect", DisconnectStyle, new Emoji("⏏"), null, disabled: false, 1);
        return builder.Build();
    }

    public static Embed GetQuizEmbed(int number, CustomPlayer player)
    {
        EmbedBuilder embedBuilder = new EmbedBuilder();
        embedBuilder.WithTitle("Playing Track: " + number);
        return embedBuilder.Build();
    }

    public static Embed GetTrackEmbed(LavalinkTrack track, CustomPlayer player)
    {
        EmbedBuilder embedBuilder = new EmbedBuilder();
        EmbedFieldBuilder fieldBuilder = new EmbedFieldBuilder();
        int totalSeconds = track.Duration.Seconds;
        int totalmins = track.Duration.Minutes;
        string Seconds = "";
        string Mins = "";
        Seconds = ((totalSeconds >= 10) ? track.Duration.Seconds.ToString() : ("0" + track.Duration.Seconds));
        Mins = ((totalmins >= 10) ? track.Duration.Minutes.ToString() : ("0" + track.Duration.Minutes));
        fieldBuilder.WithName("Duration");
        fieldBuilder.WithValue(Mins + ":" + Seconds);
        fieldBuilder.WithIsInline(isInline: true);
        embedBuilder.AddField(fieldBuilder);
        fieldBuilder = new EmbedFieldBuilder();
        fieldBuilder.WithName("Queue");
        fieldBuilder.WithValue(player.Queue.Count.ToString() ?? "");
        fieldBuilder.WithIsInline(isInline: true);
        string sourceName = "";
        if (track.SourceName != null)
        {
            sourceName = track.SourceName.ToString();
            sourceName = char.ToUpper(sourceName[0]) + sourceName.Substring(1);
        }
        if (track.Title != null)
        {
            embedBuilder.WithTitle(track.Title);
        }
        if ((object)track.Uri != null)
        {
            embedBuilder.WithUrl(track.Uri.ToString());
        }
        embedBuilder.WithColor(Color.Orange);
        if (track.Author != null)
        {
            embedBuilder.WithAuthor(track.Author.ToString());
        }
        embedBuilder.WithFooter(sourceName);
        if ((object)track.ArtworkUri != null)
        {
            embedBuilder.WithThumbnailUrl(track.ArtworkUri.ToString());
        }
        embedBuilder.AddField(fieldBuilder);
        return embedBuilder.Build();
    }

    public static async Task DeleteMessage(IUserMessage message, SocketInteractionContext Contect)
    {
        await Contect.Channel.DeleteMessageAsync(message);
    }

    public static async Task DeleteMessage(IUserMessage message, SocketInteractionContext Contect, int delay)
    {
        await Task.Delay(delay);
        await Contect.Channel.DeleteMessageAsync(message);
    }

    public static async Task Clean(CustomPlayer player, ISocketMessageChannel channel)
    {
        if (channel != null && player != null)
        {
            if (player.PlayerMessage != null)
            {
                await channel.DeleteMessageAsync(player.PlayerMessage);
            }
            if (player.LyricsMessage != null)
            {
                await channel.DeleteMessageAsync(player.LyricsMessage);
            }
            if (player.QueueMessage != null)
            {
                await channel.DeleteMessageAsync(player.QueueMessage);
            }
        }
    }

    public static Embed ScoreEmbed(Dictionary<IUser, Score> scores)
    {
        EmbedBuilder embed = new EmbedBuilder();
        embed.WithTitle("Scores");
        embed.WithAuthor("Music Quiz");
        foreach (KeyValuePair<IUser, Score> item in scores)
        {
            EmbedFieldBuilder field = new EmbedFieldBuilder();
            field.WithName(item.Key.Username);
            field.WithValue(item.Value.TotalScore);
            embed.AddField(field);
        }
        return embed.Build();
    }

    public static async Task QuizIntro(ISocketMessageChannel channel)
    {
        ComponentBuilder menuBuilder = new ComponentBuilder();
        SelectMenuBuilder roundmenu = new SelectMenuBuilder().WithPlaceholder("Number of Rounds").WithCustomId("rounds").WithMinValues(1)
            .WithMaxValues(1)
            .AddOption("1", "1", "Play one round")
            .AddOption("5", "5", "Play five rounds");
        menuBuilder.WithSelectMenu(roundmenu);
        menuBuilder.WithButton("Start", "Start", ButtonStyle.Success);
        await channel.SendMessageAsync("# Music Quiz", isTTS: false, null, null, null, null, menuBuilder.Build());
    }
}
public class Score
{
    public bool Artist = false;

    public bool Song = false;

    public int TotalScore = 0;
}

