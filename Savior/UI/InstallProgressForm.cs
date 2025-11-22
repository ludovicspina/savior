using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Savior.UI
{
    public partial class InstallProgressForm : Form
    {
        private readonly CancellationTokenSource _cts = new();
        public CancellationToken Token => _cts.Token;

        // Custom Title Bar Controls
        private Panel titleBar;
        private Label titleLabel;
        private Button closeButton;

        private ProgressBar progressBar;
        private RichTextBox logBox;
        private Button btnCancel;

        // Drag Window Logic
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        public InstallProgressForm()
        {
            InitializeComponent();
            InitializeCustomUI();
        }

        private void InitializeCustomUI()
        {
            // Form Style
            this.FormBorderStyle = FormBorderStyle.None;
            this.Width = 720;
            this.Height = 450;
            this.BackColor = Color.FromArgb(45, 45, 48); // Dark Background
            this.StartPosition = FormStartPosition.CenterParent;
            this.Padding = new Padding(2); // Border effect

            // Title Bar
            titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(30, 30, 30)
            };
            titleBar.MouseDown += TitleBar_MouseDown;

            titleLabel = new Label
            {
                Text = "Installation en cours...",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(10, 6)
            };
            titleBar.Controls.Add(titleLabel);

            closeButton = new Button
            {
                Text = "✕",
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Right,
                Width = 40,
                Cursor = Cursors.Hand
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.Red;
            closeButton.Click += (s, e) => { _cts.Cancel(); this.Close(); };
            titleBar.Controls.Add(closeButton);

            this.Controls.Add(titleBar);

            // Progress Bar
            progressBar = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 5,
                Style = ProgressBarStyle.Continuous
            };
            this.Controls.Add(progressBar);

            // Bottom Panel (Cancel Button)
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                BackColor = Color.FromArgb(45, 45, 48),
                Padding = new Padding(10)
            };

            btnCancel = new Button
            {
                Text = "Annuler",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(60, 60, 60),
                FlatStyle = FlatStyle.Flat,
                AutoSize = false,
                Size = new Size(100, 30),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Location = new Point(bottomPanel.Width - 110, 10);
            btnCancel.Click += (_, _) => { btnCancel.Enabled = false; _cts.Cancel(); Append("[INFO] Annulation demandée…"); };

            bottomPanel.Controls.Add(btnCancel);
            this.Controls.Add(bottomPanel);

            // Log Box
            logBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                DetectUrls = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.LightGray,
                BorderStyle = BorderStyle.None,
                Font = new Font("Consolas", 9),
                Padding = new Padding(10)
            };
            this.Controls.Add(logBox);
            logBox.BringToFront();
        }

        private void TitleBar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, 0xA1, 0x2, 0);
            }
        }

        public void SetSteps(int total)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => SetSteps(total))); return; }
            progressBar.Minimum = 0;
            progressBar.Maximum = Math.Max(1, total);
            progressBar.Value = 0;
        }

        public void Step()
        {
            if (InvokeRequired) { BeginInvoke(new Action(Step)); return; }
            if (progressBar.Value < progressBar.Maximum) progressBar.Value++;
        }

        public void Append(string line)
        {
            if (InvokeRequired) { BeginInvoke(new Action<string>(Append), line); return; }
            
            // Simple coloring based on content
            if (line.Contains("[ERROR]")) logBox.SelectionColor = Color.Red;
            else if (line.Contains("[WARN]")) logBox.SelectionColor = Color.Orange;
            else if (line.StartsWith("---")) logBox.SelectionColor = Color.Cyan;
            else logBox.SelectionColor = Color.LightGray;

            logBox.AppendText(line + Environment.NewLine);
            logBox.ScrollToCaret();
        }
    }
}