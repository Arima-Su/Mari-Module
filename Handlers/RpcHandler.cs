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
                .FirstOrDefault(category => category.Attribute("name")?.Value == "token")?
                .Element("entry");

            if (rpc == null)
            {
                return "fail";
            }

            discordRpcClient = new DiscordRpcClient(rpc.Value);

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

        private void UpdateRichPresence(string details)
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

        private void UpdateRichBImage(string key, string tip)
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

        private void UpdateStatusRPC(string status)
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

        public static async Task UpdateUserStatus(DiscordClient client, string state, string title)
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
                    await client.UpdateStatusAsync(new DiscordActivity(title, DSharpPlus.Entities.ActivityType.ListeningTo), DSharpPlus.Entities.UserStatus.Idle);
                }
            }
            else if (state == "IDLE")
            {
                string? status = MessageHandler.GetRandomEntry("state");

                await client.UpdateStatusAsync(new DiscordActivity(status, DSharpPlus.Entities.ActivityType.Watching), DSharpPlus.Entities.UserStatus.Idle);
            }
            else if (state == "CONCURRENT")
            {
                int num = SlashComms._queueDictionary.Count;

                await client.UpdateStatusAsync(new DiscordActivity($"in {num} servers..", DSharpPlus.Entities.ActivityType.Playing), DSharpPlus.Entities.UserStatus.Idle);
            }

            return;
        }
    }
}
