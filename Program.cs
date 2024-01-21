using Alice.Commands;
using Alice.Responses;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Lavalink;
using DSharpPlus.SlashCommands;
using Mari_Module.Handlers;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Serilog;
using System.Xml.Linq;
using Mari_Module.Properties;

namespace Mari_Module
{
    internal static class Program
    {
        // PROJECT START: 01/19/24
        // PROJECT FINISH: 01/20/24 [it was easier than expected]
        // GOAL: MIGRATE FRAMEWORK TO NET 6.0

        //DATA
        public static XDocument doc = XDocument.Load("data.xml");

        //CREDENTIALS
        private static XElement? prefix;
        public static XElement? token;
        public static XElement? weebhook;
        public static XElement? username;

        //CLIENT
        public static DiscordClient? discord;

        //PROCESSES
        public static Process? lavalink;
        private static System.Threading.Timer? disconnectionTimer;

        //MARI
        private static Mutex mutex = new Mutex(true, "Marimaru");
        private static NotifyIcon? notifyIcon;
        public static Form1? UI;
        public static bool _active = false;

        [STAThread]
        static void Main()
        {
            if (mutex.WaitOne(TimeSpan.Zero, true))
            {
                try
                {
                    ApplicationConfiguration.Initialize();

                    #region Mari rest
                    notifyIcon = new NotifyIcon();
                    notifyIcon.Icon = Resources.maru;
                    notifyIcon.Text = "maru";
                    notifyIcon.Visible = true;

                    notifyIcon.MouseDoubleClick += NotifyIcon_DoubleClick;
                    notifyIcon.MouseClick += NotifyIcon_MouseClick;
                    AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionTrapper;
                    #endregion

                    StartUpSequence().GetAwaiter().GetResult();
                    Application.Run();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(ex.ToString());
                }
            }
            else
            {
                System.Windows.MessageBox.Show("Another instance is already running..");
            }
        }

        public static async Task StartUpSequence()
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode; //CONSOLE FORMATTING

            #region Logger
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .CreateLogger();

            var logFactory = new LoggerFactory().AddSerilog();
            #endregion

            #region Credentials Extraction
            try
            {
                token = doc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == "token")?
                .Element("entry");
                prefix = doc.Descendants("category")
                    .FirstOrDefault(category => category.Attribute("name")?.Value == "prefix")?
                    .Element("entry");
                weebhook = doc.Descendants("category")
                    .FirstOrDefault(category => category.Attribute("name")?.Value == "hook")?
                    .Element("entry");
                username = doc.Descendants("category")
                    .FirstOrDefault(category => category.Attribute("name")?.Value == "username")?
                    .Element("entry");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Missing data.xml file or credentials not found.\n\n{ex}");
                return;
            }

            if (token == null || prefix == null || weebhook == null || username == null)
            {
                return;
            }
            #endregion

            #region Client Initialization
            discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = token.Value,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged | DiscordIntents.MessageContents,
                LoggerFactory = logFactory,
                MinimumLogLevel = LogLevel.Debug
            });

            discord.UseLavalink();
            #endregion

            #region RPC Initialization
            if (RpcHandler.RpcInit() == "fail")
            {
                MessageBox.Show("RPC failed to initialize, 'rpc' not found in data.xml");
                return;
            }
            #endregion

            #region Commands Initialization
            //TEXT COMMS
            var comms = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { prefix.Value },
                EnableMentionPrefix = false,
                EnableDms = false,
                EnableDefaultHelp = false
            });
            comms.RegisterCommands<Comms>();

            //SLASH COMMS
            var slash = discord.UseSlashCommands();
            slash.RegisterCommands<SlashComms>();
            #endregion

            #region Event Hooks Initialization
            discord.MessageCreated += MessageHandler.MessageCreatedHandler;
            discord.Ready += ClientReadyHandler;
            discord.VoiceStateUpdated += DisconnectionHandler;
            AppDomain.CurrentDomain.ProcessExit += ExitHandler;
            #endregion

            await discord.ConnectAsync();
        }

        private static void NotifyIcon_DoubleClick(object? sender, EventArgs e)
        {
            try
            {
                if (UI == null)
                {
                    UI = new Form1();
                }

                if (_active == false)
                {
                    UI.Show();
                    _active = true;
                }
                else
                {
                    if (UI != null)
                    {
                        UI.Close();
                        _active = false;
                        UI = null;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
        }

        private static void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Application.Exit();
            }
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e)
        {
            // This method will be called when an unhandled exception occurs
            Exception? exception = e.ExceptionObject as Exception;

            // Log the exception details, notify the user, or take other actions as needed
            if (exception == null)
            {
                return;
            }

            MessageBox.Show("Unhandled exception: " + exception.Message);
            MessageBox.Show("Stack Trace: " + exception.StackTrace);
        }

        static void ExitHandler(object? sender, EventArgs e)
        {
            if (lavalink != null && !lavalink.HasExited)
            {
                lavalink.Kill();
                lavalink.CloseMainWindow();
                lavalink.Close();
                SlashComms._lavastarted = false;
            }
        }
        private static async Task ClientReadyHandler(DiscordClient client, ReadyEventArgs e)
        {
            string? status = MessageHandler.GetRandomEntry("state");

            await client.UpdateStatusAsync(new DiscordActivity(status, DSharpPlus.Entities.ActivityType.Watching), DSharpPlus.Entities.UserStatus.Idle);

            return;
        }

        public static async Task DisconnectionHandler(DiscordClient client, VoiceStateUpdateEventArgs e)
        {
            if (e.User == client.CurrentUser)
            {
                ulong guild = e.Guild.Id;
                var guildconn = e.Guild;

                if (SlashComms._queueDictionary.ContainsKey(guild))
                {
                    SlashComms._queueDictionary.Remove(guild);
                }

                if (e.After?.Channel == null)
                {
                    if (SlashComms._queueDictionary.Count > 1)
                    {
                        Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                    }
                    else if (SlashComms._queueDictionary.Count == 1)
                    {
                        var remainingList = SlashComms._queueDictionary.Values.FirstOrDefault();

                        if (remainingList != null)
                        {
                            if (remainingList[0] != null)
                            {
                                var currentTrack = remainingList[0];

                                Console.WriteLine("PLAYER IS PLAYING");
                                Console.WriteLine($"NOW PLAYING: {currentTrack.getTrack().Title} {currentTrack.getTrack().Author}");
                                await RpcHandler.UpdateUserStatus(client, "LISTENING", $"{currentTrack.getTrack().Title} {currentTrack.getTrack().Author}");
                            }

                        }
                    }
                    else
                    {
                        Console.WriteLine("LEFT");
                        await RpcHandler.UpdateUserStatus(client, "IDLE", "bocchi");
                    }

                    var lava = client.GetLavalink();
                    var node = lava.ConnectedNodes.Values.First();
                    var conn = node.GetGuildConnection(guildconn);

                    if (PlaybackHandler.loop.Contains(guild))
                    {
                        PlaybackHandler.loop.Remove(guild);
                    }

                    if (conn != null)
                    {
                        PlaybackHandler.skipped = true;
                        await conn.StopAsync();
                        PlaybackHandler.skipped = false;
                    }

                    await Task.Delay(10);

                    if (SlashComms._queueDictionary.Count > 0)
                    {
                        Console.WriteLine("Disconnected, Keeping alive.");
                        return;
                    }

                    disconnectionTimer = new System.Threading.Timer(TimerCallback, null, TimeSpan.FromSeconds(15), Timeout.InfiniteTimeSpan);
                    Console.WriteLine("Timer Started");
                }
                else
                {
                    RemoveDisconnectionTimer();

                    if (SlashComms._queueDictionary.ContainsKey(guild) && SlashComms._queueDictionary[guild].Count > 0)
                    {
                        if (SlashComms._queueDictionary[guild][0] != null)
                        {
                            var currentTrack = SlashComms._queueDictionary[guild][0];

                            if (SlashComms._queueDictionary.Count > 1)
                            {
                                Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                            }
                            else if (SlashComms._queueDictionary.Count == 1)
                            {
                                Console.WriteLine("PLAYER IS PLAYING");
                                Console.WriteLine($"NOW PLAYING: {currentTrack.getTrack().Title} {currentTrack.getTrack().Author}");
                                await RpcHandler.UpdateUserStatus(client, "LISTENING", $"{currentTrack.getTrack().Title} {currentTrack.getTrack().Author}");
                            }
                            else
                            {
                                Console.WriteLine("JOINED");
                                await RpcHandler.UpdateUserStatus(client, "IDLE", "bocchi");
                            }
                        }
                        else
                        {
                            if (SlashComms._queueDictionary.Count > 1)
                            {
                                Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                            }
                            else
                            {
                                Console.WriteLine("JOINED");
                                await RpcHandler.UpdateUserStatus(client, "IDLE", "bocchi");
                            }
                        }
                    }
                    else
                    {
                        if (SlashComms._queueDictionary.Count > 1)
                        {
                            Console.WriteLine($"CONCURRENT: {SlashComms._queueDictionary.Count}");
                        }
                        else
                        {
                            Console.WriteLine("JOINED");
                            await RpcHandler.UpdateUserStatus(client, "IDLE", "bocchi");
                        }
                    }
                }
            }

            return;
        }

        private static void TimerCallback(object? state)
        {

            if (SlashComms._queueDictionary.Count == 0)
            {
                if (lavalink != null && !lavalink.HasExited)
                {
                    lavalink.Kill();
                    lavalink.CloseMainWindow();
                    lavalink.Close();
                    SlashComms._lavastarted = false;
                }
                Console.WriteLine("LAVALINK IS DISCONNECTED");
            }

            RemoveDisconnectionTimer();
            Console.WriteLine("Timer Ended");
        } //POTENTIAL SOURCE OF RANDOM "STATE" ERROR

        private static void RemoveDisconnectionTimer()
        {
            // Dispose of the timer to remove it
            disconnectionTimer?.Dispose();
            disconnectionTimer = null;
        }
    }
}