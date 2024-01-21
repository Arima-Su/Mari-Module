using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace Mari_Module
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Region = CreateRoundedRegion(15);
            label2.Text = DateTime.Today.Year.ToString();
            label1.Visible = false;
            label2.Visible = false;
            panel1.Visible = false;
        }

        private void Flicker()
        {
            Thread.Sleep(1000);
            panel1.Visible = true;
            label1.Visible = true;
            label2.Visible = true;
            Thread.Sleep(100);
            panel1.Visible = false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            AnimateWindow(Handle, 250, AnimationFlags.AW_BLEND | AnimationFlags.AW_HIDE);
            Program._active = false;
            Program.UI = null;
        }

        //ANIMATION CODE STARTS HERE//

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
            Flicker();
        }
    }
}