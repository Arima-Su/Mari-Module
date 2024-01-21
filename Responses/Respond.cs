﻿using DSharpPlus.EventArgs;
using DSharpPlus;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using Alice.Commands;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Alice_Module.Handlers;
using SkiaSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Alice_Module.Loaders;
using NCalc;
using System.Linq.Expressions;
using Mari_Module.Handlers;
using Mari_Module;

namespace Alice.Responses
{
    public class MessageHandler
    {
        public static string? GetRandomEntry(string category)
        {
            var xmlFilePath = "data.xml";

            if (File.Exists(xmlFilePath))
            {
                XDocument xmlDoc = XDocument.Load(xmlFilePath);

                var entries = xmlDoc.Descendants("category")
                                    .FirstOrDefault(e => e.Attribute("name")?.Value == category)
                                    ?.Elements("entry")
                                    .Select(e => e.Value)
                                    .ToList();

                if (entries != null && entries.Count > 0)
                {
                    Random random = new Random();
                    int randomIndex = random.Next(0, entries.Count);
                    return entries[randomIndex];
                }
            }

            return null;
        }

        public static async Task MessageCreatedHandler(DiscordClient client, MessageCreateEventArgs e)
        {
            // ALICE LISTENER
            
            if (e.Message.Author.IsBot && e.Message.Author.Username == "Alice")
            {
                Console.WriteLine("I heard Alice..");
                if(e.Message.Content.Contains("IP:"))
                {
                    Console.WriteLine("She gave me another IP");
                    var commandPrefix = "IP: ";
                    var IP = e.Message.Content.Substring(commandPrefix.Length).Trim();

                    if (DisComms.IPs[e.Guild.Id] == null)
                    {
                        DisComms.IPs.Add(e.Guild.Id, IP);
                    }
                    else
                    {
                        DisComms.IPs[e.Guild.Id] = IP;
                    }
                    
                }
                if (e.Message.Content.Contains("load", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said load");
                    var guild = e.Guild;
                    var messageContent = e.Message.Content;

                    var commandPrefix = "alice!load";
                    var list = messageContent.Substring(commandPrefix.Length).Trim();

                    await DisComms.LoadMusic(client, e.Message, guild, list);

                    return;
                }
                if (e.Message.Content.Contains("play", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said play..");
                    var guild = e.Guild;
                    var messageContent = e.Message.Content;

                    var commandPrefix = "alice!play";
                    var search = messageContent.Substring(commandPrefix.Length).Trim();

                    await DisComms.PlayMusic(client, e.Message, guild, search);

                    return;
                }
                if (e.Message.Content.Contains("skip", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said skip..");
                    var guild = e.Guild;

                    await DisComms.SkipMusic(client, e.Message, guild);

                    return;
                }
                if (e.Message.Content.Contains("np", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said np..");
                    var guild = e.Guild;

                    await DisComms.NpMusic(client, e.Message, guild);

                    return;
                }
                if (e.Message.Content.Contains("q", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said q");
                    var guild = e.Guild;

                    await DisComms.QueueMusic(e.Message, guild);

                    return;
                }
                if (e.Message.Content.Contains("ps", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("She said ps");
                    var guild = e.Guild;
                    var messageContent = e.Message.Content;

                    var commandPrefix = "alice!ps";
                    var search = messageContent.Substring(commandPrefix.Length).Trim();

                    await DisComms.PlaySkipMusic(client, e.Message, guild, search);

                    return;
                }
            }

            if (e.Message.Author.IsBot && e.Message.Author.Username != "Alice")
            {
                return;
            }

            // DETECTORS

            static bool _IsGreeting(string msg)
            {
                List<string> List = new List<string>
                    {
                              "Hello",
                              "Hi",
                              "Hey",
                              "Sup",
                              "Soup",
                              "Greetings",
                              "Konnichiwa",
                              "Konnichi-what's up",
                              "Ohayo",
                              "Haru"
                    };
                string pattern = $@"\b(?:{string.Join("|", List.Select(Regex.Escape))})\b";

                return Regex.IsMatch(msg, pattern, RegexOptions.IgnoreCase);
            }

            static bool _IsComplement(string msg)
            {
                List<string> List = new List<string>
                    {
                              "Good",
                              "Nice",
                              "Nice job",
                              "Good job",
                              "Naisu",
                              "Thanks",
                              "Great",
                              "Great job",
                              "Rock",
                              "Rocks",
                              "Based"
                    };
                string pattern = $@"\b(?:{string.Join("|", List.Select(Regex.Escape))})\b";

                return Regex.IsMatch(msg, pattern, RegexOptions.IgnoreCase);
            }

            static bool _IsInsult(string msg)
            {
                List<string> List = new List<string>
                    {
                              "Bad",
                              "Not Nice",
                              "Dammit",
                              "Curse",
                              "Curse you",
                              "Dang it",
                              "Damn",
                              "Biased",
                              "Frick",
                              "Frick you",
                              "Darn",
                              "Darn it",
                              "Suck",
                              "Sucks",
                              "Trash",
                              "Garbage",
                              "Acidic"
                    };
                string pattern = $@"\b(?:{string.Join("|", List.Select(Regex.Escape))})\b";

                return Regex.IsMatch(msg, pattern, RegexOptions.IgnoreCase);
            }

            // BOCCHI RESPONSES
            if (Program.username == null)
            {
                return;
            }

            if (e.Message.Content.Contains(Program.username.Value, StringComparison.OrdinalIgnoreCase) || e.Message.Content.Contains($"{Program.username.Value}'s", StringComparison.OrdinalIgnoreCase))
            {
                string keyword = Program.username.Value;
                string messageContent = e.Message.Content;

                int keywordIndex = messageContent.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                int startIndexAfter = keywordIndex + keyword.Length;

                if (keywordIndex != -1)
                {
                    string Pre = messageContent.Substring(0, keywordIndex).Trim();
                    string Suf = messageContent.Substring(startIndexAfter).Trim();

                    if (_IsGreeting(Pre))
                    {
                        string category = "Greetings";

                        string? randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string? Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }

                    if (_IsGreeting(Suf))
                    {
                        string category = "Greetings";

                        string? randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string? Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }

                    if (_IsComplement(Pre))
                    {
                        string category = "Happy_Reacts";

                        string? randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string? Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }

                    if (_IsComplement(Suf))
                    {
                        string category = "Happy_Reacts";

                        string? randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string? Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }

                    if (_IsInsult(Pre))
                    {
                        string category = "Sad_Reacts";

                        string? randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string? Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }

                    if (_IsInsult(Suf))
                    {
                        string category = "Sad_Reacts";

                        string? randomEntry = GetRandomEntry(category);

                        if (randomEntry != null)
                        {
                            await e.Message.Channel.SendMessageAsync(randomEntry);
                            return;
                        }
                        else
                        {
                            string? Entry = GetRandomEntry("Nanis");

                            if (Entry != null)
                            {
                                await e.Message.Channel.SendMessageAsync(Entry);
                                return;
                            }
                        }
                    }
                }

                if (e.Message.Content.Trim().Equals(Program.username.Value, StringComparison.OrdinalIgnoreCase) || e.Message.Content.Trim().Equals($"{Program.username.Value}?", StringComparison.OrdinalIgnoreCase) || e.Message.Content.Trim().Equals($"{Program.username.Value}.", StringComparison.OrdinalIgnoreCase))
                {
                    string? What = GetRandomEntry("Nanis");

                    if (What != null)
                    {
                        await e.Message.Channel.SendMessageAsync(What);
                    }
                }
            }

            if (e.Message.Content.Contains("Alice", StringComparison.OrdinalIgnoreCase))
            {

                string? desiredChannelId = Program.doc.Descendants("category")
                .FirstOrDefault(category => category.Attribute("name")?.Value == "channel")?
                .Element("entry")?.Value;

                if (e.Message.Channel.Id.ToString().Equals(desiredChannelId))
                {
                    string keyword = "Alice";
                    string messageContent = e.Message.Content;

                    int keywordIndex = messageContent.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);

                    if (keywordIndex != -1)
                    {
                        string extractedContent = messageContent.Substring(0, keywordIndex).Trim();

                        if (_IsGreeting(extractedContent))
                        {
                            string category = "Greetings";

                            string? randomEntry = GetRandomEntry(category);
                            string? content = randomEntry;
                            string payload = $"{{\"content\": \"{content}\"}}";

                            if (randomEntry != null)
                            {
                                using (HttpClient httpClient = new HttpClient())
                                {
                                    HttpContent httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
                                    httpClient.PostAsync(Program.weebhook?.Value, httpContent).Wait();
                                }
                            }
                            else
                            {
                                await e.Message.Channel.SendMessageAsync("No entries found for the specified category.");
                            }
                        }
                    }
                }
                else
                {
                    await e.Message.Channel.SendMessageAsync("Uhh.. She isn't here, Alice likes to stay at the canteen..");
                }
            }

            if (e.Message.Content.Contains("Kita", StringComparison.OrdinalIgnoreCase))
            {
                string category = "kita";

                string? randomEntry = GetRandomEntry(category);

                if (randomEntry != null)
                {
                    await e.Message.Channel.SendMessageAsync(randomEntry);
                }
                else
                {
                    await e.Message.Channel.SendMessageAsync("No entries found for the specified category.");
                }
            }

            if (e.Message.Content.Contains("Congrats", StringComparison.OrdinalIgnoreCase))
            {
                string category = "Celebrative_Reacts";

                string? randomEntry = GetRandomEntry(category);

                if (randomEntry != null)
                {
                    await e.Message.Channel.SendMessageAsync(randomEntry);
                }
                else
                {
                    await e.Message.Channel.SendMessageAsync("No entries found for the specified category.");
                }
            }

            if (e.Message.Content.Contains("Brother", StringComparison.OrdinalIgnoreCase))
            {
                await e.Message.Channel.SendMessageAsync("Sister even..");
            }

            if (e.Message.Content.Contains("Buddy", StringComparison.OrdinalIgnoreCase))
            {
                await e.Message.Channel.SendMessageAsync("Baka~");
            }

            if (e.Message.Content.Contains("So cool", StringComparison.OrdinalIgnoreCase))
            {
                string input = e.Message.Content;
                string target = "so cool";

                int index = input.IndexOf(target, StringComparison.OrdinalIgnoreCase);
                string extractedText = input.Substring(0, index).Trim();

                if (!e.Message.Content.Contains("Pipebomb", StringComparison.OrdinalIgnoreCase))
                {
                    if (Program.username.Value == "Bocchi")
                    {
                        var file = SKBitmap.Decode(Path.Combine("assets", "bocchinn.png"));
                        var cooledfile = Path.Combine("assets", "bocchinn_cooled.png");

                        try
                        {
                            using (var surface = SKSurface.Create(new SKImageInfo(file.Width, file.Height)))
                            {
                                using (SKCanvas canvas = surface.Canvas)
                                {
                                    canvas.DrawBitmap(file, 0, 0);

                                    using (SKPaint paint = new SKPaint())
                                    {
                                        paint.Color = SKColors.Gold;
                                        paint.TextSize = 40.0f;
                                        paint.Typeface = SKTypeface.FromFile("unispace.ttf");
                                        var point = new SKPoint(340, 746);
                                        canvas.DrawText(extractedText, point, paint);
                                    }

                                }

                                using (var coolfile = surface.Snapshot())
                                using (var data = coolfile.Encode(SKEncodedImageFormat.Png, 100))
                                using (var stream = File.OpenWrite(cooledfile))
                                {
                                    data.SaveTo(stream);
                                }
                            }

                            await save.SendSilentAsync(e.Channel.Id, cooledfile);
                        }
                        catch (Exception ex)
                        {
                            await e.Message.Channel.SendMessageAsync("https://i.imgur.com/kNh7Qlo.png");
                            Console.WriteLine(ex.Message);
                        }
                    }

                    if (Program.username.Value == "Cirno")
                    {
                        var file = SKBitmap.Decode(Path.Combine("assets", "chirumiru.png"));
                        var cooledfile = Path.Combine("assets", "chirumiru_cooled.png");

                        try
                        {
                            using (var surface = SKSurface.Create(new SKImageInfo(file.Width, file.Height)))
                            {
                                using (SKCanvas canvas = surface.Canvas)
                                {
                                    canvas.DrawBitmap(file, 0, 0);

                                    using (SKPaint paint = new SKPaint())
                                    {
                                        paint.Color = SKColors.Gold;
                                        paint.TextSize = 40.0f;
                                        paint.Typeface = SKTypeface.FromFile("unispace.ttf");
                                        var point = new SKPoint(340, 746);
                                        canvas.DrawText(extractedText, point, paint);
                                    }

                                }

                                using (var coolfile = surface.Snapshot())
                                using (var data = coolfile.Encode(SKEncodedImageFormat.Png, 100))
                                using (var stream = File.OpenWrite(cooledfile))
                                {
                                    data.SaveTo(stream);
                                }
                            }

                            await save.SendSilentAsync(e.Channel.Id, cooledfile);
                        }
                        catch (Exception ex)
                        {
                            await e.Message.Channel.SendMessageAsync("https://i.imgur.com/kNh7Qlo.png");
                            Console.WriteLine(ex.Message);
                        }
                    }
                }
            }

            if (e.Message.Content.Contains("Pipebomb", StringComparison.OrdinalIgnoreCase))
            {
                if (Program.username.Value == "Cirno")
                {
                    //await e.Message.Channel.SendMessageAsync("https://i.imgur.com/LKUiUMJ.jpg");
                    await e.Message.Channel.SendMessageAsync("https://i.imgur.com/KQBtTDN.png");
                }

                if (Program.username.Value == "Bocchi")
                {
                    //await e.Message.Channel.SendMessageAsync("https://i.imgur.com/2aeyQ8D.png");
                    await e.Message.Channel.SendMessageAsync("https://i.imgur.com/VSGi4up.png");
                }
            }

            if (e.Message.Content.Contains("Happy Cirno Day", StringComparison.OrdinalIgnoreCase))
            {
                if (Program.username.Value == "Cirno")
                {
                    await e.Message.Channel.SendMessageAsync("https://i.imgur.com/tGnKz8S.jpg");
                }
                else
                {
                    return;
                }
            }

            if (e.Message.Content.Contains("Photocopy", StringComparison.OrdinalIgnoreCase) && e.Message.Content.Contains("Bocchi", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Alright, let me get the printer..");

                if (e.Message.Attachments.Count > 0)
                {
                    foreach (var attachments in e.Message.Attachments)
                    {
                        if (attachments.FileName.EndsWith(".png") || attachments.FileName.EndsWith(".jpg") || attachments.FileName.EndsWith(".jpeg"))
                        {
                            await e.Channel.SendMessageAsync("Alright, let me get the printer..");

                            var printedfile = Path.Combine("assets", "printedfile.png");
                            var coolprinterfile = Path.Combine("assets", "coolprintedfile.png");

                            try
                            {
                                using (var httpClient = new HttpClient())
                                {
                                    using (var attachStream = await httpClient.GetStreamAsync(attachments.Url))
                                    {
                                        using (var fileStream = File.Create(printedfile))
                                        {
                                            await attachStream.CopyToAsync(fileStream);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessageAsync($"Printer error: {ex}");
                                return;
                            }

                            try
                            {
                                using (var image = SixLabors.ImageSharp.Image.Load(printedfile))
                                {
                                    image.Mutate(x => x.Grayscale());

                                    image.Mutate(x => x.BinaryThreshold(0.42f));

                                    image.Save(coolprinterfile);
                                }
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessageAsync($"Scanner error: {ex}");
                                return;
                            }

                            await save.SendSilentAsync(e.Channel.Id, coolprinterfile);
                            await e.Channel.SendMessageAsync("Here you go..");
                        }
                        else
                        {
                            await e.Message.Channel.SendMessageAsync("The printer only accepts images..");
                            break;
                        }
                    }
                }
                else
                {
                    await e.Message.Channel.SendMessageAsync("Photocopy *what* exactly?");
                }
            }

            if (e.Message.Content.Contains("solve", StringComparison.OrdinalIgnoreCase) && e.Message.Content.Any(char.IsDigit) && Validates.HasOperation(e.Message.Content))
            {
                try
                {
                    int start = -1;
                    int end = -1;

                    for (int i = 0; i < e.Message.Content.Length; i++)
                    {
                        if (char.IsDigit(e.Message.Content[i]))
                        {
                            start = i;
                            for (int j = i; j < e.Message.Content.Length; j++)
                            {
                                if ((char.IsDigit(e.Message.Content[j])) || Validates.HasOperation(e.Message.Content[j]))
                                {
                                    end = j + 1;
                                }
                                else
                                {
                                    break;
                                }
                            }
                            break;
                        }
                    }

                    if (start < 0 || end < 0)
                    {
                        await e.Message.Channel.SendMessageAsync($"well this is wack..");
                        return;
                    }

                    Console.WriteLine($"String: {e.Message.Content.Length}, Start: {start}, End: {end}");
                    var math = e.Message.Content.Substring(start, end - start);
                    Console.WriteLine($"{math.Trim()} huh..");
                    var expression = new NCalc.Expression(math).Evaluate();

                    await e.Message.Channel.SendMessageAsync($"I think the answer is: {expression}");
                }
                catch (Exception ex)
                {
                    await e.Message.Channel.SendMessageAsync($"{GetRandomEntry("Nanis")} Your math is not mathing bro.. ");
                    Console.WriteLine(ex);
                }
            }

            #region Cursed Image Generator
            //if (e.Message.Content.Contains("Photocopy", StringComparison.OrdinalIgnoreCase))
            //{
            //    Console.WriteLine("Alright, let me get the printer..");

            //    if (e.Message.Attachments.Count > 0)
            //    {
            //        foreach (var attachments in e.Message.Attachments)
            //        {
            //            if (attachments.FileName.EndsWith(".png") || attachments.FileName.EndsWith(".jpg") || attachments.FileName.EndsWith(".jpeg"))
            //            {
            //                await e.Channel.SendMessageAsync("Alright, let me get the printer..");

            //                //Stream attachStream;
            //                SKBitmap file;
            //                var printedfile = Path.Combine("assets", "printedfile.png");
            //                var coolprinterfile = Path.Combine("assets", "coolprintedfile.png");

            //                try
            //                {
            //                    using (var httpClient = new HttpClient())
            //                    {
            //                        //attachStream = await httpClient.GetStreamAsync(attachments.Url);
            //                        using (var attachStream = await httpClient.GetStreamAsync(attachments.Url))
            //                        {
            //                            using (var fileStream = File.Create(printedfile))
            //                            {
            //                                await attachStream.CopyToAsync(fileStream);
            //                            }
            //                        }
            //                    }
            //                }
            //                catch (Exception ex)
            //                {
            //                    await e.Channel.SendMessageAsync($"Printer error: {ex}");
            //                    return;
            //                }

            //                try
            //                {
            //                    file = SKBitmap.Decode(printedfile);

            //                    using (var surface = SKSurface.Create(new SKImageInfo(file.Width, file.Height)))
            //                    {
            //                        using (SKCanvas canvas = surface.Canvas)
            //                        {
            //                            canvas.DrawBitmap(file, 0, 0);

            //                            using (SKPaint paint = new SKPaint())
            //                            {
            //                                // Adjust saturation
            //                                //paint.ColorFilter = SKColorFilter.CreateColorMatrix(new float[]
            //                                //{
            //                                //    0.6f, 0, 0, 0, 0,
            //                                //    0, 0.6f, 0, 0, 0,
            //                                //    0, 0, 0.6f, 0, 0,
            //                                //    0, 0, 0, 1, 0
            //                                //});

            //                                // Adjust contrast
            //                                paint.ImageFilter = SKImageFilter.CreateColorFilter(SKColorFilter.CreateHighContrast(true, SKHighContrastConfigInvertStyle.NoInvert, 1));

            //                                // Draw the adjusted image onto the canvas
            //                                canvas.DrawBitmap(file, SKRect.Create(file.Width, file.Height), paint);
            //                            }

            //                        }

            //                        using (var coolfile = surface.Snapshot())
            //                        using (var data = coolfile.Encode(SKEncodedImageFormat.Png, 100))
            //                        using (var stream = File.OpenWrite(coolprinterfile))
            //                        {
            //                            data.SaveTo(stream);
            //                        }
            //                    }
            //                }
            //                catch (Exception ex)
            //                {
            //                    await e.Channel.SendMessageAsync($"Scanner error: {ex}");
            //                    return;
            //                }

            //                await save.SendSilentAsync(e.Channel.Id, coolprinterfile);
            //                await e.Channel.SendMessageAsync("Here you go..");
            //            }
            //            else
            //            {
            //                await e.Message.Channel.SendMessageAsync("The printer only accepts images..");
            //                break;
            //            }
            //        }
            //    }
            //    else
            //    {
            //        await e.Message.Channel.SendMessageAsync("Photocopy *what* exactly?");
            //    }
            //}
            #endregion

            return;
        }
    }
}
