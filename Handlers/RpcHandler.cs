using Alice.Commands;
using Alice.Responses;
using DiscordRPC;
using DSharpPlus.Entities;
using DSharpPlus;

namespace Mari_Module.Handlers
{
    public class RpcHandler
    {
        private static DiscordRpcClient? discordRpcClient;

        public static string RpcInit()
        {
            var rpc = Program.doc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == "rpcClient")?
                .Element("entry");

            if (rpc == null)
            {
                return "fail";
            }

            discordRpcClient = new DiscordRpcClient(rpc.Value);
            discordRpcClient.Initialize();

            try
            {
                discordRpcClient.SetPresence(new RichPresence()
                {
                    Details = MessageHandler.GetRandomEntry("idlestate"),
                    State = "",
                    Assets = new Assets()
                    {
                        LargeImageKey = "boxsleep",
                        LargeImageText = "Shh.. she's asleep~",
                        SmallImageKey = "_arimasu",
                        SmallImageText = "Hosted by _Arimasu"
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("SetRichPresence Error: " + ex.Message);
            }

            return "pass";
        }

        private static void UpdateRichPresence(string? details)
        {
            try
            {
                discordRpcClient?.UpdateDetails(details);
            }
            catch (Exception ex)
            {
                MessageBox.Show("UpdateRichPresence Error: " + ex.Message);
            }

        }

        public static void UpdateRichBImage(string key, string tip)
        {
            try
            {
                discordRpcClient?.UpdateLargeAsset(key, tip);
            }
            catch (Exception ex)
            {
                MessageBox.Show("UpdateRichImage Error: " + ex.Message);
            }
        }

        private void UpdateRichSImage(string key, string tip)
        {
            try
            {
                discordRpcClient?.UpdateLargeAsset(key, tip);
            }
            catch (Exception ex)
            {
                MessageBox.Show("UpdateRichSImage Error: " + ex.Message);
            }
        }

        private static void UpdateStatusRPC(string? status)
        {
            try
            {
                discordRpcClient?.UpdateState(status);
            }
            catch (Exception ex)
            {
                MessageBox.Show("UpdateStatusRPC Error: " + ex.Message);
            }
        }

        public static async Task UpdateUserStatus(DiscordClient? client, string state = "", string title = "", bool joined = true)
        {
            if (state == "LISTENING")
            {
                if (SlashComms._queueDictionary.Count > 1)
                {
                    if (Program.discord == null)
                    {
                        return;
                    }

                    await UpdateUserStatus(Program.discord, "CONCURRENT", "backflip");
                }
                else
                {
                    if (client == null)
                    {
                        return;
                    }

                    await client.UpdateStatusAsync(new DiscordActivity(title, DSharpPlus.Entities.ActivityType.ListeningTo), DSharpPlus.Entities.UserStatus.Idle);
                    UpdateRichPresence(MessageHandler.GetRandomEntry("activestate"));
                    UpdateStatusRPC(title);
                }
            }
            else if (state == "IDLE")
            {
                if (client == null)
                {
                    return;
                }

                if (joined)
                {
                    UpdateRichPresence(MessageHandler.GetRandomEntry("waitingstate"));
                }
                else
                {
                    UpdateRichPresence(MessageHandler.GetRandomEntry("idlestate"));
                }
                
                UpdateStatusRPC("");
                await client.UpdateStatusAsync(new DiscordActivity(MessageHandler.GetRandomEntry("state"), DSharpPlus.Entities.ActivityType.Watching), DSharpPlus.Entities.UserStatus.Idle);
            }
            else if (state == "CONCURRENT")
            {
                if (client == null)
                {
                    return;
                }

                await client.UpdateStatusAsync(new DiscordActivity($"in {SlashComms._queueDictionary.Count} servers..", DSharpPlus.Entities.ActivityType.Playing), DSharpPlus.Entities.UserStatus.Idle);
                UpdateRichPresence(MessageHandler.GetRandomEntry("activestate"));
                UpdateStatusRPC($"Playing in {SlashComms._queueDictionary.Count} servers..");
            }
            else if (state == "STARTING")
            {
                UpdateRichPresence("Getting things ready..");
            }
            else if (state == "READY")
            {
                UpdateRichPresence(MessageHandler.GetRandomEntry("waitingstate"));
                UpdateRichBImage("box", "sigh");
            }
            else if (state == "JOINED")
            {
                if (client == null)
                {
                    return;
                }

                UpdateRichPresence(MessageHandler.GetRandomEntry("joinedstate"));
                UpdateStatusRPC("");
                await client.UpdateStatusAsync(new DiscordActivity(MessageHandler.GetRandomEntry("state"), DSharpPlus.Entities.ActivityType.Watching), DSharpPlus.Entities.UserStatus.Idle);
            }
            else if (state == "LEFT")
            {
                if (client == null)
                {
                    return;
                }

                UpdateRichPresence(MessageHandler.GetRandomEntry("removedstate"));
                UpdateStatusRPC($"");
                await client.UpdateStatusAsync(new DiscordActivity(MessageHandler.GetRandomEntry("state"), DSharpPlus.Entities.ActivityType.Watching), DSharpPlus.Entities.UserStatus.Idle);
            }

            return;
        }
    }
}
