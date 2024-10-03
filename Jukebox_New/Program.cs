// Jukebox, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// Program
using System;
using System.Runtime.CompilerServices;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;

using Lavalink4NET;
using Lavalink4NET.Extensions;
using Lavalink4NET.InactivityTracking;
using Lavalink4NET.InactivityTracking.Extensions;
using Lavalink4NET.InactivityTracking.Trackers.Idle;
using Lavalink4NET.InactivityTracking.Trackers.Users;
using Lavalink4NET.Lyrics.Extensions;
using Lavalink4NET.Tracking;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


    
HostApplicationBuilder builder = new HostApplicationBuilder(args);
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<InteractionService>();
builder.Services.AddSingleton<CommandService>();


builder.Services.AddHostedService<DiscordClientHost>();
builder.Services.ConfigureLavalink(delegate (AudioServiceOptions config)
{
	config.BaseAddress = new Uri("http://127.0.0.1:2333");
    config.Passphrase = "youshallnotpass";
});

builder.Services.AddLavalink();
builder.Services.AddInactivityTracking();
builder.Services.AddLyrics();

builder.Services.AddLogging(delegate (ILoggingBuilder x)
{
        x.AddConsole().SetMinimumLevel(LogLevel.Warning);
});

builder.Services.Configure(delegate (IdleInactivityTrackerOptions config)
{
    config.Timeout = TimeSpan.FromSeconds(300.0);
});
builder.Services.Configure(delegate (UsersInactivityTrackerOptions options)
{
    options.Threshold = 1;
    options.Timeout = TimeSpan.FromSeconds(1.0);
    options.ExcludeBots = true;
});
builder.Services.ConfigureInactivityTracking(delegate (InactivityTrackingOptions options)
{
    options.DefaultTimeout = TimeSpan.FromSeconds(30.0);
    options.DefaultPollInterval = TimeSpan.FromSeconds(5.0);
    options.TrackingMode = InactivityTrackingMode.Any;
    options.UseDefaultTrackers = true;
});
builder.Build().Run();
	
