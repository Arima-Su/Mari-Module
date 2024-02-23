using Mari_Module.Handlers;
using Mari_Module.Properties;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Mari_Module
{
    public partial class Form1 : Form
    {
        private string status = "";
        private static bool running = false;
        private static bool updating = false;
        private bool isDragging;
        private Point lastLocation;

        public Form1()
        {
            InitializeComponent();
            Region = CreateRoundedRegion(15);
            label2.Text = DateTime.Today.Year.ToString();
            label3.Visible = false;
            textBox1.Visible = false;
            status = "Disconnected";

            if (Program.WiFI_desu)
            {
                label4.ForeColor = Color.MediumSpringGreen;
                status = "Connected";
            }

            this.MouseMove += Form1_MouseMove;

            var labelToolTip = new ToolTip();
            labelToolTip.SetToolTip(label4, status);
            Controls.Add(label4);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            AnimateWindow(Handle, 250, AnimationFlags.AW_BLEND | AnimationFlags.AW_HIDE);
            Program._active = false;
            Program.UI = null;
            running = false;
        }

        #region Anims
        [DllImport("user32.dll")]
        private static extern bool AnimateWindow(IntPtr hWnd, int time, AnimationFlags flags);

        // Define animation flags
        [Flags]
        private enum AnimationFlags
        {
            AW_HOR_POSITIVE = 0x00000001,
            AW_HOR_NEGATIVE = 0x00000002,
            AW_VER_POSITIVE = 0x00000004,
            AW_VER_NEGATIVE = 0x00000008,
            AW_CENTER = 0x00000010,
            AW_HIDE = 0x00010000,
            AW_ACTIVATE = 0x00020000,
            AW_SLIDE = 0x00040000,
            AW_BLEND = 0x00080000
        }

        private Region CreateRoundedRegion(int radius)
        {
            // Create a rounded rectangle path with the specified radius
            GraphicsPath path = new GraphicsPath();
            path.AddArc(0, 0, radius * 2, radius * 2, 180, 90);
            path.AddArc(Width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
            path.AddArc(Width - radius * 2, Height - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(0, Height - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();

            // Create a region based on the path
            Region region = new Region(path);

            return region;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                // Add animation flags to the form's CreateParams
                CreateParams cp = base.CreateParams;
                cp.Style |= 0x20000; // WS_MINIMIZEBOX
                cp.ClassStyle |= 0x0080; // CS_DBLCLKS
                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Animate the form with a fade-in effect
            AnimateWindow(Handle, 500, AnimationFlags.AW_BLEND);
            //Flicker();
        }
        #endregion

        #region Connection

        private void Form1_MouseMove(object? sender, MouseEventArgs e)
        {
            Rectangle bocchiArea = new Rectangle(162, 58, 311, 380); // Example: (x, y, width, height)
            Rectangle handleArea = new Rectangle(0, 0, 640, 40);

            if (handleArea.Contains(e.Location))
            {
                panel1.Visible = true;
            }
            else
            {
                panel1.Visible = false;
            }

            if (status == "Disconnected")
            {
                if (bocchiArea.Contains(e.Location))
                {
                    label3.Visible = true;
                    label3.Text = "Anyone there?\n(Attempt Connection)";
                }
                else
                {
                    label3.Visible = false;
                    label3.Text = "I'm always here, Basil..";
                }
            }
            else
            {
                if (bocchiArea.Contains(e.Location))
                {
                    label3.Visible = true;
                    label3.Text = "Not again.\n(Leave)";
                }
                else
                {
                    label3.Visible = false;
                    label3.Text = "I'm always here, Basil..";
                }
            }
        }

        private async void label3_Click(object sender, EventArgs e)
        {
            if (status == "Disconnected")
            {
                label3.Text = "Listening..\n ";

                try
                {
                    using (var client = new HttpClient())
                    {
                        var response = await client.GetAsync("http://www.google.com");
                        if (response.IsSuccessStatusCode)
                        {
                            Program.WiFI_desu = true;
                        }
                    }
                }
                catch
                {
                    await Task.Delay(1000);
                    label3.Text = "No one responded.\n ";
                    return;
                }

                if (!Program.WiFI_desu)
                {
                    await Task.Delay(1000);
                    label3.Text = "No one responded.\n ";
                }
                else
                {
                    label4.ForeColor = Color.MediumSpringGreen;

                    Program.StartUpSequence();
                    status = "Connected";
                    Controls.Remove(label4);
                    var labelToolTip = new ToolTip();
                    labelToolTip.SetToolTip(label4, status);
                    Controls.Add(label4);

                    await Task.Delay(1000);
                    label3.Text = "Something responded..\n ";
                    await Task.Delay(3000);
                    label3.Visible = false;
                }
            }
            else
            {
                this.Close();
            }
        }
        #endregion

        #region Logs

        private void label3_MouseEnter(object sender, EventArgs e)
        {
            label3.ForeColor = Color.PaleTurquoise;
        }

        private void label3_MouseLeave(object sender, EventArgs e)
        {
            label3.ForeColor = Color.FromArgb(0, 15, 25);
        }

        public static void TextWrite(string text)
        {
            if (running)
            {
                if (!textBox1.IsDisposed && textBox1.InvokeRequired)
                {
                    textBox1.Invoke((MethodInvoker)delegate
                    {
                        // Append the text to textBox1
                        textBox1.AppendText(text + Environment.NewLine);
                        textBox1.ScrollToCaret();
                    });
                }
                else
                {
                    // Update textBox1 directly if it's on the same thread
                    textBox1.AppendText(text + Environment.NewLine);
                    textBox1.ScrollToCaret();
                }
            }
        }

        private void label4_MouseEnter(object sender, EventArgs e)
        {
            textBox1.Visible = true;
            running = true;
            textBox1.Clear();
            textBox1.AppendText(Program.RiverLog.Export());
            textBox1.ScrollToCaret();
        }

        private void label4_MouseLeave(object sender, EventArgs e)
        {
            running = false;
            textBox1.Visible = false;
            textBox1.Clear();
        }

        #endregion

        #region Updates
        private void label1_MouseEnter(object sender, EventArgs e)
        {
            label1.Text = "Check for updates?";
            label1.ForeColor = Color.White;
        }

        private void label1_MouseLeave(object sender, EventArgs e)
        {
            label1.Text = "Bocchi v.3 [Mari Module]";
            label1.ForeColor = Color.PaleTurquoise;
        }

        private async void label1_Click(object sender, EventArgs e)
        {
            if (!updating)
            {
                updating = true;

                label1.Text = "Updating..";

                string releasesUrl = $"https://api.github.com/repos/Arima-Su/Bocchi/releases/latest";
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "GitHubReleaseDownloader");

                HttpResponseMessage response = await httpClient.GetAsync(releasesUrl);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();

                GitHubReleaseInfo? releaseInfo = JsonConvert.DeserializeObject<GitHubReleaseInfo>(responseBody);

                if (releaseInfo == null)
                {
                    MessageBox.Show("Update encountered an error.");
                    return;
                }

                string latestVersion = releaseInfo.tag_name;
                string versionNumber = latestVersion.Replace(".", ""); // Remove "v." and dots
                int intVersion = int.Parse(versionNumber);
                int installIndex = -1;

                if (intVersion <= Settings.Default.version)
                {
                    MessageBox.Show($"Latest version already installed. [{latestVersion}]");
                    return;
                }

                for (int i = 0; i < releaseInfo.assets.Count(); i++)
                {
                    if (releaseInfo.assets[i].name == "update.exe")
                    {
                        installIndex = i;
                        break;
                    }
                }

                if (installIndex == -1)
                {
                    MessageBox.Show("Update could not be found in the repo, contact @Arimasu for details.");
                    return;
                }

                string downloadUrl = releaseInfo.assets[installIndex].browser_download_url; // Assuming you want the first asset

                // Download the asset
                HttpResponseMessage assetResponse = await httpClient.GetAsync(downloadUrl);
                assetResponse.EnsureSuccessStatusCode();

                byte[] assetBytes = await assetResponse.Content.ReadAsByteArrayAsync();

                // Save the asset to a file
                File.WriteAllBytes(releaseInfo.assets[installIndex].name, assetBytes);

                DialogResult result = MessageBox.Show($"Update ready, install now?", "", MessageBoxButtons.OK);

                if (result == DialogResult.OK)
                {
                    Settings.Default.version = intVersion;
                    Process.Start(releaseInfo.assets[installIndex].name);
                    await Task.Delay(500);
                    Program.Die();
                }
            }
            else
            {
                MessageBox.Show("Update in progress..");
            }
        }
        #endregion

        private void panel1_MouseEnter(object sender, EventArgs e)
        {
            panel1.BackColor = Color.PaleTurquoise;
        }

        private void panel1_MouseLeave(object sender, EventArgs e)
        {
            panel1.BackColor = Color.FromArgb(0, 15, 25);
        }

        private void panel1_MouseDown(object sender, MouseEventArgs e)
        {
            isDragging = true;
            lastLocation = e.Location;
        }

        private void panel1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
        }

        private void panel1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point delta = new Point(e.Location.X - lastLocation.X, e.Location.Y - lastLocation.Y);
                this.Location = new Point(this.Location.X + delta.X, this.Location.Y + delta.Y);
            }
        }
    }
}