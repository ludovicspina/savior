using System;
using System.Threading;
using System.Windows.Forms;

namespace Savior.UI
{
    public partial class InstallProgressForm : Form
    {
        private readonly CancellationTokenSource _cts = new();
        public CancellationToken Token => _cts.Token;

        public InstallProgressForm()
        {
            InitializeComponent();
            Text = "Installations en cours";
            Width = 720; Height = 420;

            progressBar = new ProgressBar { Dock = DockStyle.Top, Height = 24, Style = ProgressBarStyle.Continuous };
            logBox = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, DetectUrls = true };
            var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 48, FlowDirection = FlowDirection.RightToLeft };
            btnCancel = new Button { Text = "Annuler", AutoSize = true };
            btnCancel.Click += (_, _) => { btnCancel.Enabled = false; _cts.Cancel(); Append("[INFO] Annulation demandée…"); };

            panel.Controls.Add(btnCancel);
            Controls.Add(logBox);
            Controls.Add(panel);
            Controls.Add(progressBar);
        }

        private ProgressBar progressBar;
        private RichTextBox logBox;
        private Button btnCancel;

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
            logBox.AppendText(line + Environment.NewLine);
            logBox.ScrollToCaret();
        }
    }
}