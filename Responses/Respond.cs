using DSharpPlus.EventArgs;
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
using Alice_Module.Loaders;
using NCalc;
using System.Linq.Expressions;
using Mari_Module.Handlers;
using Mari_Module;
using Serilog;
using System.Drawing.Imaging;
using static System.Net.Mime.MediaTypeNames;
using System.Drawing.Text;

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
            #region Alice Listener

            if (e.Message.Author.IsBot && e.Message.Author.Username == "Alice")
            {
                Log.Information("I heard Alice..");
                if(e.Message.Content.Contains("IP:"))
                {
                    Log.Information("She gave me another IP");
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
                    Log.Information("She said load");
                    var guild = e.Guild;
                    var messageContent = e.Message.Content;

                    var commandPrefix = "alice!load";
                    var list = messageContent.Substring(commandPrefix.Length).Trim();

                    await DisComms.LoadMusic(client, e.Message, guild, list);

                    return;
                }
                if (e.Message.Content.Contains("play", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("She said play..");
                    var guild = e.Guild;
                    var messageContent = e.Message.Content;

                    var commandPrefix = "alice!play";
                    var search = messageContent.Substring(commandPrefix.Length).Trim();

                    await DisComms.PlayMusic(client, e.Message, guild, search);

                    return;
                }
                if (e.Message.Content.Contains("skip", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("She said skip..");
                    var guild = e.Guild;

                    await DisComms.SkipMusic(client, e.Message, guild);

                    return;
                }
                if (e.Message.Content.Contains("np", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("She said np..");
                    var guild = e.Guild;

                    await DisComms.NpMusic(client, e.Message, guild);

                    return;
                }
                if (e.Message.Content.Contains("q", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("She said q");
                    var guild = e.Guild;

                    await DisComms.QueueMusic(e.Message, guild);

                    return;
                }
                if (e.Message.Content.Contains("ps", StringComparison.OrdinalIgnoreCase))
                {
                    Log.Information("She said ps");
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
            #endregion

            #region Detectors
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
                              "What's up",
                              "Whats up",
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
                              "Naisu",
                              "Nice job",
                              "Good job",
                              "Naisu",
                              "Thanks",
                              "Thank",
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
                              "Dangit",
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

            static bool _IsCommand(string msg)
            {
                List<string> List = new List<string>
                    {
                              "Photocopy",
                              "Solve"
                    };
                string pattern = $@"\b(?:{string.Join("|", List.Select(Regex.Escape))})\b";

                return Regex.IsMatch(msg, pattern, RegexOptions.IgnoreCase);
            }
            #endregion

            #region Reactions
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
                    else if (_IsGreeting(Suf))
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
                    else if (_IsComplement(Pre))
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
                    else if (_IsComplement(Suf))
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
                    else if (_IsInsult(Pre))
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
                    else if (_IsInsult(Suf))
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
                    else if (_IsCommand(Suf) || _IsCommand(Pre))
                    {
                        Console.WriteLine("I'll let the other logic catch this one..");
                    }
                    else
                    {
                        string? What = GetRandomEntry("Nanis");

                        if (What != null)
                        {
                            await e.Message.Channel.SendMessageAsync(What);
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

            #endregion

            #region Alice
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
            #endregion

            #region Keywords
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

            if (e.Message.Content.Contains("Ubel", StringComparison.OrdinalIgnoreCase))
            {
                string category = "ubel";

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

            if (e.Message.Content.Contains("Bocchi", StringComparison.OrdinalIgnoreCase) && e.Message.Content.Contains("Tank", StringComparison.OrdinalIgnoreCase))
            {
                await e.Message.Channel.SendMessageAsync("https://i.imgur.com/ept6Eh1.jpeg");
            }
            #endregion

            #region GIFS
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
                        var file = Path.Combine("assets", "bocchinn.png");
                        var cooledfile = Path.Combine("assets", "bocchinn_cooled.png");

                        try
                        {
                            Bitmap inputImage = new Bitmap(file);

                            using (Graphics graphics = Graphics.FromImage(inputImage))
                            {
                                using (Font font = new Font(LoadFont("unispace.ttf"), 40))
                                using (SolidBrush brush = new SolidBrush(Color.Gold))
                                {
                                    Point textLocation = new Point(330, 690);
                                    graphics.DrawString(extractedText, font, brush, textLocation);
                                }
                            }

                            inputImage.Save(cooledfile, System.Drawing.Imaging.ImageFormat.Png);

                            await save.SendSilentAsync(e.Channel.Id, cooledfile);
                        }
                        catch (Exception ex)
                        {
                            await e.Message.Channel.SendMessageAsync("https://i.imgur.com/kNh7Qlo.png");
                            Log.Information(ex.Message);
                        }
                    }

                    if (Program.username.Value == "Cirno")
                    {
                        var file = Path.Combine("assets", "chirumiru.png");
                        var cooledfile = Path.Combine("assets", "chirumiru_cooled.png");

                        try
                        {
                            Bitmap inputImage = new Bitmap(file);

                            using (Graphics graphics = Graphics.FromImage(inputImage))
                            {
                                using (Font font = new Font(LoadFont("unispace.ttf"), 40))
                                using (SolidBrush brush = new SolidBrush(Color.Gold))
                                {
                                    Point textLocation = new Point(330, 690);
                                    graphics.DrawString(extractedText, font, brush, textLocation);
                                }
                            }

                            inputImage.Save(cooledfile, System.Drawing.Imaging.ImageFormat.Png);

                            await save.SendSilentAsync(e.Channel.Id, cooledfile);
                        }
                        catch (Exception ex)
                        {
                            await e.Message.Channel.SendMessageAsync("https://i.imgur.com/kNh7Qlo.png");
                            Log.Information(ex.Message);
                        }
                    }
                }
            }

            static FontFamily LoadFont(string path)
            {
                // Load a custom font from file
                PrivateFontCollection fontCollection = new PrivateFontCollection();
                fontCollection.AddFontFile(path);
                return fontCollection.Families[0];
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
#endregion

            #region Photocopy
            if (e.Message.Content.Contains("Photocopy", StringComparison.OrdinalIgnoreCase) && e.Message.Content.Contains("Bocchi", StringComparison.OrdinalIgnoreCase))
            {
                Log.Information("Alright, let me get the printer..");

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
                                Bitmap inputImage = new Bitmap(Path.Combine("assets", "printedfile.png"));

                                // Threshold brightness level (adjust as needed)
                                int threshold = 128;

                                for (int x = 0; x < inputImage.Width; x++)
                                {
                                    for (int y = 0; y < inputImage.Height; y++)
                                    {
                                        Color pixelColor = inputImage.GetPixel(x, y);
                                        int brightness = (int)(0.299 * pixelColor.R + 0.587 * pixelColor.G + 0.114 * pixelColor.B);

                                        Color newColor = brightness < threshold ? Color.Black : Color.White;
                                        inputImage.SetPixel(x, y, newColor);
                                    }
                                }

                                inputImage.Save(coolprinterfile);
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessageAsync($"Printer error: {ex}");
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
            #endregion

            #region Solve
            if (e.Message.Content.Contains("solve", StringComparison.OrdinalIgnoreCase) && e.Message.Content.Any(char.IsDigit) && Validates.HasOperation(e.Message.Content))
            {
                try
                {
                    string msg = e.Message.Content;
                    char[] msgcontents = msg.ToCharArray();
                    var equation = new List<char>();

                    //EXTRACT EQUATION FROM MSG
                    for (int i = 0; i < msgcontents.Length; i++)
                    {
                        if (char.IsDigit(msgcontents[i]) || msgcontents[i] == '(' || msgcontents[i] == ')' || Validates.HasOperation(e.Message.Content[i]))
                        {
                            equation.Add(msgcontents[i]);
                        }
                    }

                    var log = new StringBuilder();

                    for (int i = 0; i < equation.Count; i++)
                    {
                        log.Append(equation[i] + " ");
                    }

                    await e.Message.Channel.SendMessageAsync(log.ToString());

                    //FORMAT EQUATION
                    var numbers = new List<int>();
                    var digits = new List<int>();

                    for (int i = 0; i < equation.Count; i++)
                    {
                        int num = 0;

                        while (char.IsDigit(equation[i]))
                        {
                            digits.Add((equation[i] - '0'));
                        }

                        for (int k = 0; k < digits.Count; k++)
                        {
                            //FIGURE OUT HOW TO 1 = 1; 2 = 10; 3 = 100; 4 = 1000
                            int multiplier = 10 ^ (digits.Count - k);
                            num += digits[k] * multiplier;
                        }

                        numbers.Add(num);
                        await e.Message.Channel.SendMessageAsync(num.ToString());
                    }
                }
                catch (Exception ex)
                {
                    await e.Message.Channel.SendMessageAsync(ex.ToString());
                }

                //try
                //{
                //    int start = -1;
                //    int end = -1;

                //    for (int i = 0; i < e.Message.Content.Length; i++)
                //    {
                //        if (char.IsDigit(e.Message.Content[i]))
                //        {
                //            start = i;
                //            for (int j = i; j < e.Message.Content.Length; j++)
                //            {
                //                if ((char.IsDigit(e.Message.Content[j])) || Validates.HasOperation(e.Message.Content[j]))
                //                {
                //                    end = j + 1;
                //                }
                //                else
                //                {
                //                    break;
                //                }
                //            }
                //            break;
                //        }
                //    }

                //    if (start < 0 || end < 0)
                //    {
                //        await e.Message.Channel.SendMessageAsync($"well this is wack..");
                //        return;
                //    }

                //    Log.Information($"String: {e.Message.Content.Length}, Start: {start}, End: {end}");
                //    var math = e.Message.Content.Substring(start, end - start);
                //    Log.Information($"{math.Trim()} huh..");
                //    var expression = new NCalc.Expression(math).Evaluate();

                //    await e.Message.Channel.SendMessageAsync($"I think the answer is: {expression}");
                //}
                //catch (Exception ex)
                //{
                //    await e.Message.Channel.SendMessageAsync($"{GetRandomEntry("Nanis")} Your math is not mathing bro.. ");
                //    Log.Information(ex);
                //}
            }
            #endregion

            #region Cursed Image Generator
            //if (e.Message.Content.Contains("Photocopy", StringComparison.OrdinalIgnoreCase))
            //{
            //    Log.Information("Alright, let me get the printer..");

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
