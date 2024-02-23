using DSharpPlus.Lavalink.EventArgs;
using DSharpPlus.Lavalink;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Alice.Commands;
using Alice.Responses;
using Alice_Module.Loaders;
using YoutubeExplode;
using YoutubeExplode.Common;
using System.Diagnostics;
using Serilog;

namespace Mari_Module.Handlers
{
    public class PlaybackHandler
    {
        public static bool skipped;
        public static bool forcestop = false; //TO BE UPDATED, CURRENTLY NOT CONCURRENT COMPAT
        public static bool unbroken = false; //TO BE UPDATED, CURRENTLY NOT CONCURRENT COMPAT
        public static List<ulong> freeplaylist = new List<ulong>();
        public static List<ulong> reconlist = new List<ulong>();
        public static List<ulong> loop = new List<ulong>();

        public static Task PlaybackErrorHandler(LavalinkGuildConnection sender, TrackExceptionEventArgs e)
        {
            Log.Information("I fumbled a song..");
            return Task.CompletedTask;
        }

        public static async Task RetryAsync(LavalinkGuildConnection conn, LavalinkTrack track)
        {
            Log.Information("Retried..");
            try
            {
                skipped = true;
                await conn.PlayAsync(track);

                if (Program.discord == null)
                {
                    return;
                }

                if (SlashComms._queueDictionary.Count > 1)
                {
                    await RpcHandler.UpdateUserStatus(Program.discord, "CONCURRENT", "backflip");
                    Log.Information($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                }
                else
                {
                    await RpcHandler.UpdateUserStatus(Program.discord, "LISTENING", $"{track.Title} {track.Author}");
                    Log.Information($"NOW PLAYING: {track.Title} {track.Author}");
                }
                skipped = false;
            }
            catch
            {
                Log.Information("Well that failed, I'll try that again..");
                await RetryAsync(conn, track);
            }
        }

        public static async Task StartLava()
        {
            Program.lavalink = new Process();
            Program.lavalink.StartInfo.FileName = @"C:\Program Files\Java\jdk-11\bin\java.exe";
            Program.lavalink.StartInfo.Arguments = "-jar Lavalink.jar";
            Program.lavalink.StartInfo.RedirectStandardOutput = true;
            Program.lavalink.StartInfo.UseShellExecute = false;
            Program.lavalink.StartInfo.CreateNoWindow = true;

            var taskCompletionSource = new TaskCompletionSource<bool>();

            void OutputDataReceivedHandler(object sender, DataReceivedEventArgs args)
            {
                if (Program.lavalink == null || args.Data == null)
                {
                    return;
                }

                if (args.Data.Contains("Lavalink is ready to accept connections."))
                {
                    taskCompletionSource.SetResult(true);

                    // Unsubscribe from the event
                    Program.lavalink.OutputDataReceived -= OutputDataReceivedHandler;
                }
                if (args.Data.Contains("Web server failed to start"))
                {
                    taskCompletionSource.SetResult(true);
                    SlashComms._failed = true;

                    // Unsubscribe from the event
                    Program.lavalink.OutputDataReceived -= OutputDataReceivedHandler;
                }
            }

            // Subscribe to the event
            Program.lavalink.OutputDataReceived += OutputDataReceivedHandler;

            Program.lavalink.Start();
            Program.lavalink.BeginOutputReadLine();

            await taskCompletionSource.Task;
        }

        public static async Task PlaybackFinishedHandler(LavalinkGuildConnection sender, TrackFinishEventArgs e)
        {
            ulong guild = sender.Guild.Id;

            if (skipped == true)
            {
                if (!freeplaylist.Contains(guild))
                {
                    return;
                }
            }
            else
            {
                if (freeplaylist.Contains(guild) && !reconlist.Contains(guild))
                {
                    Log.Information("FREE BIRD YEAHH..");
                    await sender.Channel.SendMessageAsync("Hmmm.. let me see here..");
                    var youtube = new YoutubeClient();

                    //GET ORIGINAL VIDEO
                    var videos = await youtube.Search.GetPlaylistsAsync(SlashComms._queueDictionary[guild][0].getTrack().Title + " " + SlashComms._queueDictionary[guild][0].getTrack().Author);

                    //GET PLAYLIST
                    if (videos.Count > 0)
                    {
                        Random random = new Random();
                        int randomIndex = random.Next(0, videos.Count);

                        var songTitles = await PlayLoader.YoutubeLoaderAsync(videos[randomIndex].Url);

                        if (songTitles == null || songTitles.Count == 0)
                        {
                            await sender.Channel.SendMessageAsync("No song titles found in the playlist.");
                            return;
                        }

                        await sender.Channel.SendMessageAsync("Loading Playlist..");
                        var progressMessage = await sender.Channel.SendMessageAsync("_ songs queue'd..");
                        int songCount = 0;

                        foreach (string title in songTitles)
                        {
                            unbroken = true;

                            if (PlayLoader._queuefull == true)
                            {
                                PlayLoader._queuefull = false;
                                break;
                            }

                            if (forcestop == true)
                            {
                                unbroken = false;
                                break;
                            }

                            if (songCount > 9)
                            {
                                break;
                            }

                            ++songCount;

                            await PlayLoader.FreeEnqueue(sender, title);
                            await progressMessage.ModifyAsync($"{songCount} songs queue'd..");
                        }

                        await progressMessage.ModifyAsync("Playlist loaded.");
                        Log.Information("Theoretically, it should be playing..");
                        SlashComms._queueDictionary[guild].RemoveAt(0);
                        reconlist.Add(guild);
                    }
                    else
                    {
                        await sender.Channel.SendMessageAsync("I can't think of a next song to recommend..");
                        Log.Information("Could not get playlist");
                        SlashComms._queueDictionary[guild].RemoveAt(0);
                    }

                    return;
                }

                if (SlashComms._queueDictionary[guild] != null)
                {
                    if (SlashComms._queueDictionary[guild].Count > 0)
                    {
                        if (loop.Contains(guild))
                        {
                            var loopTrack = SlashComms._queueDictionary[guild][0];
                            skipped = true;
                            await sender.PlayAsync(loopTrack.getTrack());
                            skipped = false;
                        }
                        else
                        {
                            if (SlashComms._queueDictionary[guild].Count == 1)
                            {
                                SlashComms._queueDictionary[guild].RemoveAt(0);
                                SlashComms._queueDictionary.Remove(guild);
                                Log.Information("SONG ENDED");
                                Log.Information("JOINED");
                                string? status = MessageHandler.GetRandomEntry("state");

                                if (status == null || Program.discord == null)
                                {
                                    return;
                                }

                                await RpcHandler.UpdateUserStatus(Program.discord, "JOINED");
                                return;
                            }
                            else
                            {
                                var nextTrack = SlashComms._queueDictionary[guild][1];

                                if (SlashComms._queueDictionary[guild].Count == 2 && freeplaylist.Contains(guild))
                                {
                                    reconlist.Remove(guild);
                                }

                                SlashComms._queueDictionary[guild].RemoveAt(0);

                                try
                                {
                                    skipped = true;
                                    await sender.PlayAsync(nextTrack.getTrack());

                                    if (Program.discord == null)
                                    {
                                        return;
                                    }

                                    if (SlashComms._queueDictionary.Count > 1)
                                    {
                                        await RpcHandler.UpdateUserStatus(Program.discord, "CONCURRENT", "backflip");
                                        Log.Information($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                                    }
                                    else
                                    {
                                        await RpcHandler.UpdateUserStatus(Program.discord, "LISTENING", $"{nextTrack.getTrack().Title} {nextTrack.getTrack().Author}");
                                        Log.Information($"NOW PLAYING: {nextTrack.getTrack().Title} {nextTrack.getTrack().Author}");
                                    }
                                    skipped = false;
                                }
                                catch
                                {
                                    await RetryAsync(sender, nextTrack.getTrack());
                                }
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
        }
    }
}
