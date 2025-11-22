using System.ComponentModel;
using System.Diagnostics;
using System.IO.Compression;
using Savior.Services;
using Savior.Constants;
using System.Text;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace Savior.UI
{
    public partial class MainForm : Form
    {
        // Drag Window Logic
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private Panel _customTitleBar;
        private Label _customTitleLabel;
        private Button _btnClose;
        private Button _btnMinimize;
        private HardwareMonitorService _hardwareMonitor;
        private SystemInfoService _systemInfo;

        private System.Windows.Forms.Button _buttonActivation;
        
        private CancellationTokenSource? stressCancellationTokenSource = null;
        private bool isCpuStressRunning = false;
        private bool isGpuStressRunning = false;


        private string _windowsActivationStatus;

        private System.Windows.Forms.Label labelCPURef2;
        private System.Windows.Forms.Label labelGPURef2;
        private System.Windows.Forms.Label labelRAM;
        private System.Windows.Forms.Label labelCPUCores;
        private System.Windows.Forms.Label labelDisk;


        private CheckedListBox checkedListBoxServices;
        private CheckedListBox checkedListBoxApps;
        private Button buttonOptimisation;
        private Button buttonBloatWare;

        private System.Windows.Forms.CheckBox checkBoxVLC;
        private CheckBox checkBox7ZIP;
        private System.Windows.Forms.CheckBox checkBoxChrome;
        private System.Windows.Forms.CheckBox checkBoxAdobe;
        private CheckBox checkBoxSublimeText;
        private System.Windows.Forms.CheckBox checkBoxLibreOffice;
        private CheckBox checkBoxKaspersky;
        private CheckBox checkBoxBitdefender;
        private CheckBox checkBoxSteam;
        private CheckBox checkBoxDiscord;
        private CheckBox checkBoxTeams;
        private CheckBox checkBoxTreeSize;
        private System.Windows.Forms.CheckBox checkBoxHDDS;
        private CheckBox checkBoxLenovoVantage;
        private CheckBox checkBoxMyAsus;
        private CheckBox checkBoxHpSmart;

        private System.Windows.Forms.Label labelManu;

        private System.Windows.Forms.Label labelWindowsActivation;

        private Label labelGPU;
        private Label labelCpuTemp;
        private Label labelGpuTemp;

        private ToolStripStatusLabel toolStripStatusLabelWindows;

        private CheckedListBox checkedListBoxCleanupTools;
        private Button btnCleanup;

        public MainForm()
        {
            InitializeComponent();
            if (IsInDesignMode())
                return;
            this.Icon = new Icon("Data/savior.ico");
            this.Load += MainForm_Load;
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            if (IsInDesignMode())
                return;

            InitializeServices();
            RefreshTemperatures();
            LoadOptimizationLists();
            ApplyTheme();

            var timer = new System.Windows.Forms.Timer { Interval = 500 };
            timer.Tick += (_, _) => RefreshTemperatures();
            timer.Start();

            _ = Task.Run(() =>
            {
                try
                {
                    var cpu = _systemInfo.GetCpuInfo();
                    var gpu = _systemInfo.GetGpuInfo();
                    var ram = _systemInfo.GetRamInfo();
                    var disk = _systemInfo.GetDiskInfo();
                    var manu = _systemInfo.GetManuInfo();

                    Console.WriteLine(disk);


                    Invoke(() =>
                    {
                        labelCPURef2.Text = cpu.Name;
                        labelCPUCores.Text =
                            $"Cœurs logiques : {cpu.LogicalCores} | Cœurs physiques : {cpu.PhysicalCores}";
                        labelRAM.Text = "RAM installée : " + ram + " Go";
                        labelDisk.Text = "Disques :\r\n" + disk;
                        labelGPURef2.Text = gpu;
                        labelManu.Text = manu;
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erreur chargement info système : " + ex.Message);
                }
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    await CheckWindowsActivationStatusAsync();

                    Invoke(() =>
                    {
                        if (toolStripStatusLabelWindows != null)
                            toolStripStatusLabelWindows.Text = _windowsActivationStatus;
                        if (labelWindowsActivation != null)
                            labelWindowsActivation.Text = $"Windows : {_windowsActivationStatus}";
                    });
                }
                catch (Exception ex)
                {
                    Invoke(() =>
                    {
                        if (labelWindowsActivation != null)
                            labelWindowsActivation.Text = $"Erreur : {ex.Message}";
                        else
                            Console.WriteLine($"Erreur (pas de label dispo) : {ex.Message}");
                    });
                }
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    string localVersionPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "version.txt");
                    string localVersion = File.Exists(localVersionPath)
                        ? File.ReadAllText(localVersionPath).Trim()
                        : "0.0.0";

                    using var client = new HttpClient();
                    string remoteVersion = await client.GetStringAsync("http://deifall.com/savior-updater/version.txt");
                    remoteVersion = remoteVersion.Trim();

                    if (localVersion != remoteVersion)
                    {
                        Invoke(() =>
                        {
                            buttonMaj.Visible = true;
                            buttonMaj.Text = $"Mettre à jour ({localVersion} → {remoteVersion})";
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("⚠️ Erreur vérification MAJ : " + ex.Message);
                }
            });
        }

        private void ApplyTheme()
        {
            Color darkBg = Color.FromArgb(45, 45, 48);
            Color darkerBg = Color.FromArgb(30, 30, 30);
            Color lightText = Color.White;
            Color btnBg = Color.FromArgb(60, 60, 60);

            // 1. Remove standard border
            this.FormBorderStyle = FormBorderStyle.None;
            this.Padding = new Padding(2); // Border effect
            this.BackColor = darkBg;
            this.ForeColor = lightText;

            // 2. Create Custom Title Bar if not exists
            if (_customTitleBar == null)
            {
                _customTitleBar = new Panel
                {
                    Dock = DockStyle.Top,
                    Height = 32,
                    BackColor = darkerBg
                };
                _customTitleBar.MouseDown += (s, e) => {
                    if (e.Button == MouseButtons.Left)
                    {
                        ReleaseCapture();
                        SendMessage(Handle, 0xA1, 0x2, 0);
                    }
                };

                _btnClose = new Button
                {
                    Text = "✕",
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Dock = DockStyle.Right,
                    Width = 40,
                    Cursor = Cursors.Hand
                };
                _btnClose.FlatAppearance.BorderSize = 0;
                _btnClose.FlatAppearance.MouseOverBackColor = Color.Red;
                _btnClose.Click += (s, e) => this.Close();

                _btnMinimize = new Button
                {
                    Text = "—",
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    FlatStyle = FlatStyle.Flat,
                    Dock = DockStyle.Right,
                    Width = 40,
                    Cursor = Cursors.Hand
                };
                _btnMinimize.FlatAppearance.BorderSize = 0;
                _btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(80, 80, 80);
                _btnMinimize.Click += (s, e) => this.WindowState = FormWindowState.Minimized;

                _customTitleLabel = new Label
                {
                    Text = "Savior",
                    ForeColor = Color.White,
                    Font = new Font("Segoe UI", 10, FontStyle.Regular),
                    AutoSize = true,
                    Location = new Point(10, 6)
                };

                _customTitleBar.Controls.Add(_customTitleLabel);
                _customTitleBar.Controls.Add(_btnMinimize);
                _customTitleBar.Controls.Add(_btnClose);

                this.Controls.Add(_customTitleBar);
            }

            // 3. Theme all controls
            foreach (Control c in this.Controls)
            {
                ThemeControl(c, darkBg, darkerBg, lightText, btnBg);
            }
        }

        private void ThemeControl(Control c, Color darkBg, Color darkerBg, Color text, Color btnBg)
        {
            if (c is Button btn)
            {
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = btnBg;
                btn.ForeColor = text;
                btn.FlatAppearance.BorderSize = 0;
            }
            else if (c is Label lbl)
            {
                lbl.ForeColor = text;
            }
            else if (c is CheckBox cb)
            {
                cb.ForeColor = text;
            }
            else if (c is GroupBox gb)
            {
                gb.ForeColor = text;
            }
            else if (c is CheckedListBox clb)
            {
                clb.BackColor = darkerBg;
                clb.ForeColor = text;
                clb.BorderStyle = BorderStyle.None;
            }
            else if (c is TabControl tc)
            {
                foreach (TabPage page in tc.TabPages)
                {
                    page.BackColor = darkBg;
                    page.ForeColor = text;
                    foreach (Control child in page.Controls)
                    {
                        ThemeControl(child, darkBg, darkerBg, text, btnBg);
                    }
                }
            }

            if (c.HasChildren && !(c is TabControl))
            {
                foreach (Control child in c.Controls)
                {
                    ThemeControl(child, darkBg, darkerBg, text, btnBg);
                }
            }
        }

        private void InitializeServices()
        {
            if (IsInDesignMode())
                return;

            _hardwareMonitor = new HardwareMonitorService();
            _systemInfo = new SystemInfoService();
        }
        
        private Task CreateShortcutsAsync()
        {
            SetDesktopSystemIcons(true); // 👈 affiche les 4 icônes système
            return Task.CompletedTask;
        }
        
        private async Task ActivateWindowsIfNeededAsync()
        {
            if (!_windowsActivationStatus.Contains("Actif"))
            {
                string masPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "MAS_AIO.cmd");
                if (!File.Exists(masPath))
                {
                    MessageBox.Show("MAS_AIO.cmd non trouvé.");
                    return;
                }

                Process.Start(new ProcessStartInfo
                {
                    FileName = masPath,
                    WorkingDirectory = Path.GetDirectoryName(masPath),
                    UseShellExecute = true,
                    Verb = "runas"
                });

                await Task.Delay(1000);
            }
        }
        
        private async Task RunFullSetupAsync(List<string> wingetApps)
        {
            using var dlg = new Savior.UI.InstallProgressForm();
            dlg.Show(this);

            try
            {
                // 1. Remove bloatwares
                await RemoveBloatwaresAsync(dlg.Append, dlg.Token);

                // 2. Disable services
                await DisableUnwantedServicesAsync(dlg.Append, dlg.Token);

                // 3. Winget installation
                dlg.Append("--- Installation des applications (Winget) ---");
                dlg.Append("Vérification / réparation de winget…");
                await Savior.Services.WingetInstaller.EnsureWingetHealthyAsync(dlg.Append);

                dlg.SetSteps(wingetApps.Count);
                await Savior.Services.WingetInstaller.InstallSelectedWithProgressAsync(
                    wingetApps, dlg.Append, dlg.Step, dlg.Token
                );

                dlg.Append("--- Setup complet ! ---");
            }
            catch (Exception ex)
            {
                dlg.Append("[ERROR] " + ex.Message);
                MessageBox.Show(ex.Message, "Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        

        
        private async Task RemoveBloatwaresAsync(Action<string> log, CancellationToken token)
        {
            var script = new StringBuilder();

            // --- Corps : désinstallations sélectionnées ---
            foreach (var item in checkedListBoxApps.Items)
            {
                if (checkedListBoxApps.GetItemChecked(checkedListBoxApps.Items.IndexOf(item)))
                {
                    string appName = item.ToString();
                    script.AppendLine($@"
$package = Get-AppxPackage -Name '{appName}' -ErrorAction SilentlyContinue
if ($package) {{
    Remove-AppxPackage $package -ErrorAction SilentlyContinue
    Write-Output '{appName} -> Désinstallé'
}} else {{
    Write-Output '{appName} -> Non trouvé'
}}");
                }
            }

            if (script.Length > 0)
            {
                log("--- Suppression des bloatwares ---");
                string tempFile = Path.Combine(Path.GetTempPath(), $"RemoveBloat_{Guid.NewGuid()}.ps1");
                File.WriteAllText(tempFile, script.ToString());

                try
                {
                    await ProcessRunner.RunHiddenAsync(
                        "powershell.exe",
                        $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"",
                        log
                    );
                }
                finally
                {
                    if (File.Exists(tempFile)) File.Delete(tempFile);
                }
            }
            else
            {
                log("Aucun bloatware sélectionné.");
            }
        }


        private async Task DisableUnwantedServicesAsync(Action<string> log, CancellationToken token)
        {
            var selectedServices = new List<string>();

            foreach (var item in checkedListBoxServices.Items)
            {
                if (checkedListBoxServices.GetItemChecked(checkedListBoxServices.Items.IndexOf(item)))
                {
                    selectedServices.Add(item.ToString());
                }
            }

            if (selectedServices.Any())
            {
                log("--- Désactivation des services ---");
                string scriptPath =
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "DisableServices.ps1");

                if (!File.Exists(scriptPath))
                {
                    log("[ERROR] Script DisableServices.ps1 introuvable.");
                    return;
                }

                string joinedServices = string.Join(",", selectedServices.Select(s => s.Replace("'", "''"));

                await ProcessRunner.RunHiddenAsync(
                    "powershell.exe",
                    $"-NoProfile -ExecutionPolicy Bypass -File \"{scriptPath}\" -Services \"{joinedServices}\"",
                    log
                );
            }
            else
            {
                log("Aucun service sélectionné.");
            }
        }

        private void RunPowerShellScript(string scriptPathOrContent)
        {
            string tempFile = scriptPathOrContent;

            if (!File.Exists(scriptPathOrContent))
            {
                // Si on passe un contenu directement, l’écrire dans un fichier temporaire
                tempFile = Path.Combine(Path.GetTempPath(), $"tmp_{Guid.NewGuid()}.ps1");
                File.WriteAllText(tempFile, scriptPathOrContent);
            }

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{tempFile}\"",
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(psi);
        }

        private async Task RunCleanupAsync(List<CleanupTool> selectedTools, Action<string> log, CancellationToken token)
        {
            log("=== Démarrage du nettoyage ===");
            
            foreach (var tool in selectedTools)
            {
                if (token.IsCancellationRequested)
                {
                    log("[WARN] Nettoyage annulé par l'utilisateur");
                    break;
                }

                log($"--- {tool.Name} ---");
                log(tool.Description);

                try
                {
                    // Check if it's a PowerShell script (multiline or contains PowerShell cmdlets)
                    bool isPowerShell = tool.Command.Contains("\n") || 
                                       tool.Command.Contains("Remove-Item") ||
                                       tool.Command.Contains("Get-") ||
                                       tool.Command.Contains("Clear-RecycleBin") ||
                                       tool.Command.Contains("Optimize-Volume");

                    if (isPowerShell)
                    {
                        // Execute as PowerShell script
                        string tempFile = Path.Combine(Path.GetTempPath(), $"Cleanup_{Guid.NewGuid()}.ps1");
                        File.WriteAllText(tempFile, tool.Command);

                        try
                        {
                            await ProcessRunner.RunHiddenAsync(
                                "powershell.exe",
                                $"-NoProfile -ExecutionPolicy Bypass -File \"{tempFile}\"",
                                log
                            );
                        }
                        finally
                        {
                            if (File.Exists(tempFile)) File.Delete(tempFile);
                        }
                    }
                    else
                    {
                        // Execute as direct command
                        var parts = tool.Command.Split(' ', 2);
                        string exe = parts[0];
                        string args = parts.Length > 1 ? parts[1] : "";

                        await ProcessRunner.RunHiddenAsync(exe, args, log);
                    }

                    log($"✓ {tool.Name} terminé");
                }
                catch (Exception ex)
                {
                    log($"[ERROR] {tool.Name} a échoué : {ex.Message}");
                }

                log(""); // Empty line for separation
            }

            log("=== Nettoyage terminé ===");
        }

        private void LoadCleanupTools()
        {
            checkedListBoxCleanupTools.Items.Clear();
            
            foreach (var tool in CleanupTools.All)
            {
                // Pre-check basic cleanup tools
                bool isChecked = tool.Category == CleanupCategory.Basic;
                checkedListBoxCleanupTools.Items.Add(tool.Name, isChecked);
            }
        }

        private void LoadOptimizationLists()
        {
            // Pour les applications
            checkedListBoxApps.Items.Clear();
            foreach (var app in BloatwareLists.Apps)
            {
                bool isChecked = app.Contains("BingSports") ||
                                 app.Contains("BingFinance") ||
                                 app.Contains("BingWeather") ||
                                 app.Contains("ZuneVideo") ||
                                 app.Contains("ZuneMusic") ||
                                 app.Contains("MicrosoftSolitaireCollection") ||
                                 app.Contains("3DBuilder") ||
                                 app.Contains("OneConnect") ||
                                 app.Contains("SkypeApp");
                checkedListBoxApps.Items.Add(app, isChecked);
            }

            // Pour les services
            checkedListBoxServices.Items.Clear();
            foreach (var service in BloatwareLists.Services)
            {
                bool isChecked = service switch
                {
                    "DiagTrack" => true,
                    "dmwappushservice" => true,
                    "RetailDemo" => true,
                    "Fax" => true,
                    _ => false
                };
                checkedListBoxServices.Items.Add(service, isChecked);
            }

            // Load cleanup tools
            LoadCleanupTools();
        }

        private async void BtnCleanup_Click(object sender, EventArgs e)
        {
            try
            {
                // Get selected tools
                var selectedToolNames = new List<string>();
                foreach (var item in checkedListBoxCleanupTools.CheckedItems)
                {
                    selectedToolNames.Add(item.ToString());
                }

                if (!selectedToolNames.Any())
                {
                    MessageBox.Show("Veuillez sélectionner au moins un outil de nettoyage.", "Nettoyage", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Find the corresponding CleanupTool objects
                var selectedTools = CleanupTools.All
                    .Where(t => selectedToolNames.Contains(t.Name))
                    .ToList();

                // Check if any heavy operations are selected
                bool hasHeavyOps = selectedTools.Any(t => 
                    t.Name.Contains("SFC") || 
                    t.Name.Contains("DISM") || 
                    t.Name.Contains("Windows Update Cleanup"));

                if (hasHeavyOps)
                {
                    var result = MessageBox.Show(
                        "⚠️ Vous avez sélectionné des opérations lourdes (SFC, DISM) qui peuvent prendre 15-30 minutes.\n\n" +
                        "Voulez-vous continuer ?",
                        "Avertissement",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result != DialogResult.Yes)
                        return;
                }

                // Run cleanup in progress window
                using var dlg = new Savior.UI.InstallProgressForm();
                dlg.Show(this);

                await RunCleanupAsync(selectedTools, dlg.Append, dlg.Token);

                MessageBox.Show("✅ Nettoyage terminé !", "Nettoyage", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Erreur lors du nettoyage : {ex.Message}", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LaunchPowerShellScript(string scriptContent)
        {
            try
            {
                string tempScriptPath = Path.Combine(Path.GetTempPath(), $"SaviorScript_{Guid.NewGuid()}.ps1");
                File.WriteAllText(tempScriptPath, scriptContent);

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoExit -ExecutionPolicy Bypass -File \"{tempScriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas" // Admin obligatoire
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'exécution du script PowerShell : {ex.Message}");
            }
        }

        private void RefreshTemperatures()
        {
            // Console.WriteLine(">>> Refreshing temps...");

            float cpuRealTemp = _hardwareMonitor.GetCpuRealTemperature();
            // Console.WriteLine("CPU TEMP: " + cpuRealTemp);

            var gpuTemps = _hardwareMonitor.GetGpuTemperatures();
            var gpuTempText = gpuTemps.Count > 0
                ? string.Join("  ", gpuTemps.Select(t => $"GPU: {t.Value:F1} °C"))
                : "GPU: 0.0 °C";

            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    labelCpuTemp.Text = $"CPU: {cpuRealTemp:F1} °C";
                    labelGpuTemp.Text = gpuTempText;
                }));
            }
            else
            {
                labelCpuTemp.Text = $"CPU: {cpuRealTemp:F1} °C";
                labelGpuTemp.Text = gpuTempText;
            }

            if (float.IsNaN(cpuRealTemp))
                Console.WriteLine("⚠️ Température CPU non trouvée");
        }
        
        private async void ButtonUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                buttonMaj.Enabled = false;
                buttonMaj.Text = "Téléchargement...";

                string zipUrl = "http://deifall.com/savior-updater/Savior.zip";
                string zipPath = Path.Combine(Path.GetTempPath(), "Savior_Update.zip");
                string extractPath = Path.Combine(Path.GetTempPath(), "Savior_Extracted");
                string appFolder = AppDomain.CurrentDomain.BaseDirectory;

                using var client = new HttpClient();
                var data = await client.GetByteArrayAsync(zipUrl);
                await File.WriteAllBytesAsync(zipPath, data);

                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);
                ZipFile.ExtractToDirectory(zipPath, extractPath);

                foreach (string file in Directory.GetFiles(extractPath, "*", SearchOption.AllDirectories))
                {
                    string relativePath = Path.GetRelativePath(extractPath, file);
                    string destPath = Path.Combine(appFolder, relativePath);

                    Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                    File.Copy(file, destPath, true);
                }

                Directory.Delete(extractPath, true);
                File.Delete(zipPath);

                MessageBox.Show("✅ Mise à jour installée. L’application va redémarrer.");

                Process.Start(Path.Combine(appFolder, "Savior.exe"));
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Erreur MAJ : " + ex.Message);
            }
        }

        private async Task EnsureWingetInstalledAsync()
        {
            try
            {
                // 1) Vérifie si winget existe déjà
                var psiCheck = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var p = Process.Start(psiCheck))
                {
                    if (p != null)
                    {
                        string output = await p.StandardOutput.ReadToEndAsync();
                        await p.WaitForExitAsync();
                        if (p.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                        {
                            Console.WriteLine("✅ Winget déjà installé : " + output.Trim());
                            return; // winget dispo → rien à faire
                        }
                    }
                }
            }
            catch
            {
                Console.WriteLine("Winget non trouvé, installation requise...");
            }

            // 2) Installe App Installer (.msixbundle)
            string bundlePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle");

            if (!File.Exists(bundlePath))
            {
                MessageBox.Show("⚠️ Le fichier App Installer est introuvable : " + bundlePath);
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -Command \"Add-AppxPackage -Path '{bundlePath}'\"",
                UseShellExecute = true,
                Verb = "runas" // admin requis
            };

            Process.Start(psi);

            MessageBox.Show("📦 Installation de App Installer lancée.\nRelance l’application une fois terminée.");
        }


        private async void BtnMultimediaSetup_Click(object sender, EventArgs e)
        {
            try
            {
                // await CreateShortcutsAsync(); // Création des raccourcis bureau
                // await EnsureWingetInstalledAsync(); // MAJ Winget obligatoire pour éviter les confits
                // OpenSettingsUri("ms-settings:windowsupdate"); // Ouverture de windows update
                // await ActivateWindowsIfNeededAsync(); // Ouverture de MAS si besoin d'activer windows
                // await GpuInstaller.InstallForDetectedGpuAsync(msg => Console.WriteLine(msg)); // Installation des GPU si besoin
                await RunFullSetupAsync(AppProfiles.Multimedia); // All-in-one setup
                
                

                // MessageBox.Show("Setup multimédia terminé ✅");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur dans le setup multimédia : {ex.Message}");
            }
        }
        
        private async void BtnGamingSetup_Click(object sender, EventArgs e)
        {
            try
            {
                await CreateShortcutsAsync(); // Création des raccourcis bureau
                await EnsureWingetInstalledAsync(); // MAJ Winget obligatoire pour éviter les confits
                OpenSettingsUri("ms-settings:windowsupdate"); // Ouverture de windows update
                await ActivateWindowsIfNeededAsync(); // Ouverture de MAS si besoin d'activer windows
                // await GpuInstaller.InstallForDetectedGpuAsync(msg => Console.WriteLine(msg)); // Installation des GPU si besoin
                await RunFullSetupAsync(AppProfiles.Gaming); // All-in-one setup
                
                

                // MessageBox.Show("Setup multimédia terminé ✅");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur dans le setup multimédia : {ex.Message}");
            }
        }

        private static void OpenSettingsUri(string uri)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = uri, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d’ouvrir les Paramètres : {ex.Message}");
            }
        }


        private async void InstallNvidiaGpu_Click(object sender, EventArgs e)
        {
            btnInstallGpuSuite.Enabled = false;
            try
            {
                await Savior.Services.GpuInstaller.InstallNvidiaGpu(msg =>
                {
                    // log vers une TextBox multi-lignes par ex.
                    // txtLog.AppendText(msg + Environment.NewLine);
                });
            }
            finally
            {
                btnInstallGpuSuite.Enabled = true;
            }
        }
        
        private async void InstallAmdGpu_Click(object sender, EventArgs e)
        {
            btnInstallGpuSuite.Enabled = false;
            try
            {
                await Savior.Services.GpuInstaller.InstallAmdGpu(msg =>
                {
                    // log vers une TextBox multi-lignes par ex.
                    // txtLog.AppendText(msg + Environment.NewLine);
                });
            }
            finally
            {
                btnInstallGpuSuite.Enabled = true;
            }
        }

        private void BtnDisableSelectedServices_Click(object sender, EventArgs e)
        {
            if (checkedListBoxServices.CheckedItems.Count == 0)
            {
                MessageBox.Show("Aucun service sélectionné !");
                return;
            }

            string psCommand = "";

            foreach (var item in checkedListBoxServices.CheckedItems)
            {
                string serviceName = item.ToString();
                psCommand += $@"
$service = Get-Service -Name '{serviceName}' -ErrorAction SilentlyContinue
if ($service) {{
    try {{
        Stop-Service -Name '{serviceName}' -Force -ErrorAction SilentlyContinue
        Set-Service -Name '{serviceName}' -StartupType Disabled
        Write-Output '{serviceName} -> Désactivé'
    }} catch {{
        Write-Output '{serviceName} -> Erreur lors de la désactivation'
    }}
}} else {{
    Write-Output '{serviceName} -> Non trouvé'
}}
";
            }

            LaunchPowerShellScript(psCommand);
        }
        

        private void BtnActivateWindows_Click(object sender, EventArgs e)
        {
            try
            {
                string masPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Scripts", "MAS_AIO.cmd");
                string masDir = Path.GetDirectoryName(masPath)!; // Dossier du script
                Console.WriteLine("Chemin du script MAS : " + masPath);


                if (!File.Exists(masPath))
                {
                    MessageBox.Show("Le fichier MAS_AIO.cmd est introuvable. Chemin : " + masPath);
                    return;
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = masPath, // 🆕 Chemin complet du script
                    WorkingDirectory = masDir, // 🆕 Dossier du script
                    UseShellExecute = true,
                    Verb = "runas" // Exécution en tant qu'admin
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message);
            }
        }
        
        private async void BtnOpenPowerShell_Click(object sender, EventArgs e)
        {
            try
            {
                var selection = new Dictionary<string, bool>
                {
                    ["VLC"] = checkBoxVLC?.Checked == true,
                    ["7ZIP"] = checkBox7ZIP?.Checked == true,
                    ["Chrome"] = checkBoxChrome?.Checked == true,
                    ["Adobe"] = checkBoxAdobe?.Checked == true,
                    ["SublimeText"] = checkBoxSublimeText?.Checked == true,
                    ["LibreOffice"] = checkBoxLibreOffice?.Checked == true,
                    ["Bitdefender"] = checkBoxBitdefender?.Checked == true,
                    ["Steam"] = checkBoxSteam?.Checked == true,
                    ["Discord"] = checkBoxDiscord?.Checked == true,
                    ["Teams"] = checkBoxTeams?.Checked == true,
                    ["TreeSize"] = checkBoxTreeSize?.Checked == true,
                    ["HDDS"] = checkBoxHDDS?.Checked == true,
                    ["LenovoVantage"] = checkBoxLenovoVantage?.Checked == true,
                    ["HpSmart"] = checkBoxHpSmart?.Checked == true,
                    ["MyAsus"] = checkBoxMyAsus?.Checked == true
                };

                // Exception : Kaspersky reste un lien web
                if (checkBoxKaspersky?.Checked == true)
                    Process.Start(new ProcessStartInfo { FileName = "https://www.kaspersky.fr/downloads/standard", UseShellExecute = true });

                await Savior.Services.WingetInstaller.InstallSelectedAsync(selection, msg => Console.WriteLine(msg));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur installations: {ex.Message}");
            }
        }
        
        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const uint SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_IDLIST = 0x0000;

        private static void SetDesktopSystemIcons(bool show)
        {
            var guids = new[]
            {
                "{20D04FE0-3AEA-1069-A2D8-08002B30309D}", // This PC
                "{645FF040-5081-101B-9F08-00AA002F954E}", // Recycle Bin
                "{59031a47-3f72-44a7-89c5-5595fe6b30ee}", // User Files
                "{5399E694-6CE5-4D6C-8FCE-1D8870FDCBA0}"  // Control Panel
            };

            void SetFor(string subkey)
            {
                using var key = Registry.CurrentUser.CreateSubKey(subkey, true)!;
                foreach (var g in guids)
                    key.SetValue(g, show ? 0 : 1, RegistryValueKind.DWord);
            }

            SetFor(@"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\NewStartPanel");
            SetFor(@"Software\Microsoft\Windows\CurrentVersion\Explorer\HideDesktopIcons\ClassicStartMenu");

            // rafraîchit Explorer (sans le tuer)
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }



        private async Task CheckWindowsActivationStatusAsync()
        {
            try
            {
                string activationStatus = "❓ Inconnu";
                string script = @"
Get-WmiObject -Query 'SELECT Name, LicenseStatus FROM SoftwareLicensingProduct WHERE PartialProductKey IS NOT NULL' |
    Select-Object -Property Name, LicenseStatus";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-NoProfile -ExecutionPolicy Bypass -Command \"" + script.Replace("\"", "`\"") + "\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                string output = await RunProcessAsync(psi);

                // Analyse de la sortie
                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                var statusLines = lines
                    .Where(l => l.Contains("Windows"))
                    .ToList();


                // Recherche du premier produit avec statut actif
                foreach (var line in statusLines)
                {
                    Console.WriteLine(line);
                    if (line.Contains("1"))
                    {
                        activationStatus = "✅ Actif";
                        break;
                    }

                    if (line.Contains("0"))
                    {
                        activationStatus = "❌ Inactif";
                    }
                }

                _windowsActivationStatus = activationStatus;
            }
            catch (Exception ex)
            {
                _windowsActivationStatus = $"❌ Erreur : {ex.Message}";
            }
        }

        private async Task<string> RunProcessAsync(ProcessStartInfo startInfo)
        {
            using (var process = new Process { StartInfo = startInfo })
            {
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();
                return output;
            }
        }


        private bool IsInDesignMode()
        {
            return LicenseManager.UsageMode == LicenseUsageMode.Designtime || DesignMode;
        }

        private void BtnUninstallSelectedApps_Click(object sender, EventArgs e)
        {
            if (checkedListBoxApps.CheckedItems.Count == 0)
            {
                MessageBox.Show("Aucune application sélectionnée !");
                return;
            }

            string psCommand = "";

            foreach (var item in checkedListBoxApps.CheckedItems)
            {
                string appName = item.ToString();
                psCommand += $@"
$package = Get-AppxPackage -Name '{appName}' -ErrorAction SilentlyContinue
if ($package) {{
    Remove-AppxPackage $package -ErrorAction SilentlyContinue
    Write-Output '{appName} -> Désinstallé'
}} else {{
    Write-Output '{appName} -> Non trouvé'
}}
";
            }

            LaunchPowerShellScript(psCommand);
        }

        private async void btnInstallGpuSuite_Click(object sender, EventArgs e)
        {
            btnInstallGpuSuite.Enabled = false;
            try
            {
                await Savior.Services.GpuInstaller.InstallForDetectedGpuAsync(msg =>
                {
                    // log vers une TextBox multi-lignes par ex.
                    // txtLog.AppendText(msg + Environment.NewLine);
                });
            }
            finally
            {
                btnInstallGpuSuite.Enabled = true;
            }
        }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            AllTabs = new Savior.UI.DarkTabControl();
            TabGeneral = new System.Windows.Forms.TabPage();
            buttonMaj = new System.Windows.Forms.Button();
            groupBox11 = new System.Windows.Forms.GroupBox();
            AMDInstall = new System.Windows.Forms.Button();
            NvidiaInstall = new System.Windows.Forms.Button();
            buttonGamingInstallGeneral = new System.Windows.Forms.Button();
            btnInstallGpuSuite = new System.Windows.Forms.Button();
            buttonBasicInstallGeneral = new System.Windows.Forms.Button();
            groupBox5 = new System.Windows.Forms.GroupBox();
            labelManu = new System.Windows.Forms.Label();
            labelDisk = new System.Windows.Forms.Label();
            labelGPURef2 = new System.Windows.Forms.Label();
            labelCPURef2 = new System.Windows.Forms.Label();
            labelCPUCores = new System.Windows.Forms.Label();
            labelRAM = new System.Windows.Forms.Label();
            TabSoftwares = new System.Windows.Forms.TabPage();
            groupBox4 = new System.Windows.Forms.GroupBox();
            checkBoxSteam = new System.Windows.Forms.CheckBox();
            checkBoxDiscord = new System.Windows.Forms.CheckBox();
            groupBox3 = new System.Windows.Forms.GroupBox();
            checkBoxKaspersky = new System.Windows.Forms.CheckBox();
            checkBoxBitdefender = new System.Windows.Forms.CheckBox();
            groupBox2 = new System.Windows.Forms.GroupBox();
            checkBoxHDDS = new System.Windows.Forms.CheckBox();
            checkBoxTreeSize = new System.Windows.Forms.CheckBox();
            groupBox1 = new System.Windows.Forms.GroupBox();
            checkBoxMyAsus = new System.Windows.Forms.CheckBox();
            checkBoxHpSmart = new System.Windows.Forms.CheckBox();
            checkBoxLenovoVantage = new System.Windows.Forms.CheckBox();
            checkBoxSublimeText = new System.Windows.Forms.CheckBox();
            checkBox7ZIP = new System.Windows.Forms.CheckBox();
            checkBoxTeams = new System.Windows.Forms.CheckBox();
            checkBoxVLC = new System.Windows.Forms.CheckBox();
            checkBoxAdobe = new System.Windows.Forms.CheckBox();
            checkBoxLibreOffice = new System.Windows.Forms.CheckBox();
            checkBoxChrome = new System.Windows.Forms.CheckBox();
            InstallSelection = new System.Windows.Forms.Button();
            tabOptimisation = new System.Windows.Forms.TabPage();
            groupBox7 = new System.Windows.Forms.GroupBox();
            checkedListBoxServices = new System.Windows.Forms.CheckedListBox();
            buttonOptimisation = new System.Windows.Forms.Button();
            groupBox6 = new System.Windows.Forms.GroupBox();
            checkedListBoxApps = new System.Windows.Forms.CheckedListBox();
            buttonBloatWare = new System.Windows.Forms.Button();
            tabVirus = new System.Windows.Forms.TabPage();
            checkedListBoxCleanupTools = new System.Windows.Forms.CheckedListBox();
            btnCleanup = new System.Windows.Forms.Button();
            labelCpuTemp = new System.Windows.Forms.Label();
            labelGpuTemp = new System.Windows.Forms.Label();
            labelWindowsActivation = new System.Windows.Forms.Label();
            _buttonActivation = new System.Windows.Forms.Button();
            // Removed previously added controls (btnVirusClean, txtVirusLog) to restore original state
            AllTabs.SuspendLayout();
            TabGeneral.SuspendLayout();
            groupBox11.SuspendLayout();
            groupBox5.SuspendLayout();
            TabSoftwares.SuspendLayout();
            groupBox4.SuspendLayout();
            groupBox3.SuspendLayout();
            groupBox2.SuspendLayout();
            groupBox1.SuspendLayout();
            tabOptimisation.SuspendLayout();
            groupBox7.SuspendLayout();
            groupBox6.SuspendLayout();
            tabVirus.SuspendLayout();
            SuspendLayout();
            // 
            // AllTabs
            // 
            AllTabs.AccessibleName = "AllTabs";
            AllTabs.Controls.Add(TabGeneral);
            AllTabs.Controls.Add(TabSoftwares);
            AllTabs.Controls.Add(tabOptimisation);
            AllTabs.Controls.Add(tabVirus);
            AllTabs.Dock = System.Windows.Forms.DockStyle.Top;
            AllTabs.Location = new System.Drawing.Point(0, 0);
            AllTabs.Name = "AllTabs";
            AllTabs.SelectedIndex = 0;
            AllTabs.Size = new System.Drawing.Size(897, 608);
            AllTabs.TabIndex = 0;
            // 
            // TabGeneral
            // 
            TabGeneral.Controls.Add(labelCpuTemp);
            TabGeneral.Controls.Add(labelGpuTemp);
            TabGeneral.Controls.Add(labelWindowsActivation);
            TabGeneral.Controls.Add(_buttonActivation);
            TabGeneral.Controls.Add(buttonMaj);
            TabGeneral.Controls.Add(groupBox11);
            TabGeneral.Controls.Add(groupBox5);
            TabGeneral.Location = new System.Drawing.Point(4, 28);
            TabGeneral.Name = "TabGeneral";
            TabGeneral.Padding = new System.Windows.Forms.Padding(3);
            TabGeneral.Size = new System.Drawing.Size(889, 576);
            TabGeneral.TabIndex = 0;
            TabGeneral.Text = "General";
            TabGeneral.UseVisualStyleBackColor = true;
            // 
            // buttonMaj
            // 
            buttonMaj.ForeColor = System.Drawing.Color.Green;
            buttonMaj.Location = new System.Drawing.Point(290, 540);
            buttonMaj.Name = "buttonMaj";
            buttonMaj.Size = new System.Drawing.Size(307, 30);
            buttonMaj.TabIndex = 6;
            buttonMaj.Text = "MAJ";
            buttonMaj.UseVisualStyleBackColor = true;
            buttonMaj.Visible = false;
            buttonMaj.Click += ButtonUpdate_Click;
            // 
            // groupBox11
            // 
            groupBox11.Controls.Add(AMDInstall);
            groupBox11.Controls.Add(NvidiaInstall);
            groupBox11.Controls.Add(buttonGamingInstallGeneral);
            groupBox11.Controls.Add(btnInstallGpuSuite);
            groupBox11.Controls.Add(buttonBasicInstallGeneral);
            groupBox11.Location = new System.Drawing.Point(520, 17);
            groupBox11.Name = "groupBox11";
            groupBox11.Size = new System.Drawing.Size(361, 251);
            groupBox11.TabIndex = 3;
            groupBox11.TabStop = false;
            groupBox11.Text = "Installations";
            // 
            // AMDInstall
            // 
            AMDInstall.BackColor = System.Drawing.Color.Transparent;
            AMDInstall.Font = new System.Drawing.Font("Segoe UI", 8.765218F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)0));
            AMDInstall.ForeColor = System.Drawing.Color.FromArgb(((int)((byte)192)), ((int)((byte)0)), ((int)((byte)0)));
            AMDInstall.Location = new System.Drawing.Point(110, 216);
            AMDInstall.Name = "AMDInstall";
            AMDInstall.Size = new System.Drawing.Size(98, 29);
            AMDInstall.TabIndex = 8;
            AMDInstall.Text = "Adrenalin";
            AMDInstall.UseVisualStyleBackColor = false;
            AMDInstall.Click += InstallAmdGpu_Click;
            // 
            // NvidiaInstall
            // 
            NvidiaInstall.Font = new System.Drawing.Font("Segoe UI", 8.765218F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)0));
            NvidiaInstall.ForeColor = System.Drawing.Color.LimeGreen;
            NvidiaInstall.Location = new System.Drawing.Point(6, 216);
            NvidiaInstall.Name = "NvidiaInstall";
            NvidiaInstall.Size = new System.Drawing.Size(98, 29);
            NvidiaInstall.TabIndex = 8;
            NvidiaInstall.Text = "Nvidia App";
            NvidiaInstall.UseVisualStyleBackColor = true;
            NvidiaInstall.Click += InstallNvidiaGpu_Click;
            // 
            // buttonGamingInstallGeneral
            // 
            buttonGamingInstallGeneral.Location = new System.Drawing.Point(56, 73);
            buttonGamingInstallGeneral.Name = "buttonGamingInstallGeneral";
            buttonGamingInstallGeneral.Size = new System.Drawing.Size(257, 32);
            buttonGamingInstallGeneral.TabIndex = 7;
            buttonGamingInstallGeneral.Text = "Setup Gaming";
            buttonGamingInstallGeneral.UseVisualStyleBackColor = true;
            buttonGamingInstallGeneral.Click += BtnGamingSetup_Click;
            // 
            // btnInstallGpuSuite
            // 
            btnInstallGpuSuite.Location = new System.Drawing.Point(257, 216);
            btnInstallGpuSuite.Name = "btnInstallGpuSuite";
            btnInstallGpuSuite.Size = new System.Drawing.Size(98, 29);
            btnInstallGpuSuite.TabIndex = 6;
            btnInstallGpuSuite.Text = "Auto GPU";
            btnInstallGpuSuite.UseVisualStyleBackColor = true;
            btnInstallGpuSuite.Click += btnInstallGpuSuite_Click;
            // 
            // buttonBasicInstallGeneral
            // 
            buttonBasicInstallGeneral.Location = new System.Drawing.Point(56, 35);
            buttonBasicInstallGeneral.Name = "buttonBasicInstallGeneral";
            buttonBasicInstallGeneral.Size = new System.Drawing.Size(257, 32);
            buttonBasicInstallGeneral.TabIndex = 2;
            buttonBasicInstallGeneral.Text = "Setup Multimedia";
            buttonBasicInstallGeneral.UseVisualStyleBackColor = true;
            buttonBasicInstallGeneral.Click += BtnMultimediaSetup_Click;
            // 
            // groupBox5
            // 
            groupBox5.BackColor = System.Drawing.Color.Transparent;
            groupBox5.Controls.Add(labelManu);
            groupBox5.Controls.Add(labelDisk);
            groupBox5.Controls.Add(labelGPURef2);
            groupBox5.Controls.Add(labelCPURef2);
            groupBox5.Controls.Add(labelCPUCores);
            groupBox5.Controls.Add(labelRAM);
            groupBox5.Location = new System.Drawing.Point(19, 17);
            groupBox5.Name = "groupBox5";
            groupBox5.Size = new System.Drawing.Size(483, 251);
            groupBox5.TabIndex = 0;
            groupBox5.TabStop = false;
            groupBox5.Text = "Composants";
            // 
            // labelManu
            // 
            labelManu.Location = new System.Drawing.Point(325, 19);
            labelManu.Name = "labelManu";
            labelManu.Size = new System.Drawing.Size(152, 23);
            labelManu.TabIndex = 2;
            labelManu.Text = "MANUFACTURER";
            labelManu.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelDisk
            // 
            labelDisk.Location = new System.Drawing.Point(6, 100);
            labelDisk.Name = "labelDisk";
            labelDisk.Size = new System.Drawing.Size(439, 128);
            labelDisk.TabIndex = 8;
            labelDisk.Text = "DISK NOT FOUND";
            // 
            // labelGPURef2
            // 
            labelGPURef2.Location = new System.Drawing.Point(6, 59);
            labelGPURef2.Name = "labelGPURef2";
            labelGPURef2.Size = new System.Drawing.Size(301, 20);
            labelGPURef2.TabIndex = 5;
            labelGPURef2.Text = "GPU NOT FOUND";
            labelGPURef2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCPURef2
            // 
            labelCPURef2.Location = new System.Drawing.Point(6, 19);
            labelCPURef2.Name = "labelCPURef2";
            labelCPURef2.Size = new System.Drawing.Size(301, 20);
            labelCPURef2.TabIndex = 4;
            labelCPURef2.Text = "CPU NOT FOUND";
            labelCPURef2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelCPUCores
            // 
            labelCPUCores.Location = new System.Drawing.Point(6, 39);
            labelCPUCores.Name = "labelCPUCores";
            labelCPUCores.Size = new System.Drawing.Size(301, 20);
            labelCPUCores.TabIndex = 7;
            labelCPUCores.Text = "CORES NOT FOUND";
            labelCPUCores.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // labelRAM
            // 
            labelRAM.Location = new System.Drawing.Point(6, 80);
            labelRAM.Name = "labelRAM";
            labelRAM.Size = new System.Drawing.Size(301, 20);
            labelRAM.TabIndex = 6;
            labelRAM.Text = "RAM NOT FOUND";
            labelRAM.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TabSoftwares
            // 
            TabSoftwares.Controls.Add(groupBox4);
            TabSoftwares.Controls.Add(groupBox3);
            TabSoftwares.Controls.Add(groupBox2);
            TabSoftwares.Controls.Add(groupBox1);
            TabSoftwares.Controls.Add(InstallSelection);
            TabSoftwares.Location = new System.Drawing.Point(4, 28);
            TabSoftwares.Name = "TabSoftwares";
            TabSoftwares.Padding = new System.Windows.Forms.Padding(3);
            TabSoftwares.Size = new System.Drawing.Size(889, 576);
            TabSoftwares.TabIndex = 1;
            TabSoftwares.Text = "Softwares";
            TabSoftwares.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            groupBox4.Controls.Add(checkBoxSteam);
            groupBox4.Controls.Add(checkBoxDiscord);
            groupBox4.Location = new System.Drawing.Point(499, 74);
            groupBox4.Name = "groupBox4";
            groupBox4.Size = new System.Drawing.Size(155, 415);
            groupBox4.TabIndex = 14;
            groupBox4.TabStop = false;
            groupBox4.Text = "Gaming";
            // 
            // checkBoxSteam
            // 
            checkBoxSteam.Location = new System.Drawing.Point(6, 22);
            checkBoxSteam.Name = "checkBoxSteam";
            checkBoxSteam.Size = new System.Drawing.Size(104, 24);
            checkBoxSteam.TabIndex = 9;
            checkBoxSteam.Text = "Steam";
            checkBoxSteam.UseVisualStyleBackColor = true;
            // 
            // checkBoxDiscord
            // 
            checkBoxDiscord.Location = new System.Drawing.Point(6, 52);
            checkBoxDiscord.Name = "checkBoxDiscord";
            checkBoxDiscord.Size = new System.Drawing.Size(104, 24);
            checkBoxDiscord.TabIndex = 10;
            checkBoxDiscord.Text = "Discord";
            checkBoxDiscord.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            groupBox3.Controls.Add(checkBoxKaspersky);
            groupBox3.Controls.Add(checkBoxBitdefender);
            groupBox3.Location = new System.Drawing.Point(338, 74);
            groupBox3.Name = "groupBox3";
            groupBox3.Size = new System.Drawing.Size(155, 415);
            groupBox3.TabIndex = 13;
            groupBox3.TabStop = false;
            groupBox3.Text = "Antivirus";
            // 
            // checkBoxKaspersky
            // 
            checkBoxKaspersky.Location = new System.Drawing.Point(6, 22);
            checkBoxKaspersky.Name = "checkBoxKaspersky";
            checkBoxKaspersky.Size = new System.Drawing.Size(104, 24);
            checkBoxKaspersky.TabIndex = 7;
            checkBoxKaspersky.Text = "Kaspersky";
            checkBoxKaspersky.UseVisualStyleBackColor = true;
            // 
            // checkBoxBitdefender
            // 
            checkBoxBitdefender.Location = new System.Drawing.Point(6, 52);
            checkBoxBitdefender.Name = "checkBoxBitdefender";
            checkBoxBitdefender.Size = new System.Drawing.Size(104, 24);
            checkBoxBitdefender.TabIndex = 8;
            checkBoxBitdefender.Text = "Bit Defender";
            checkBoxBitdefender.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(checkBoxHDDS);
            groupBox2.Controls.Add(checkBoxTreeSize);
            groupBox2.Location = new System.Drawing.Point(660, 74);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new System.Drawing.Size(155, 415);
            groupBox2.TabIndex = 12;
            groupBox2.TabStop = false;
            groupBox2.Text = "Autres";
            // 
            // checkBoxHDDS
            // 
            checkBoxHDDS.Location = new System.Drawing.Point(6, 52);
            checkBoxHDDS.Name = "checkBoxHDDS";
            checkBoxHDDS.Size = new System.Drawing.Size(131, 24);
            checkBoxHDDS.TabIndex = 1;
            checkBoxHDDS.Text = "HD Sentinel";
            checkBoxHDDS.UseVisualStyleBackColor = true;
            // 
            // checkBoxTreeSize
            // 
            checkBoxTreeSize.Location = new System.Drawing.Point(6, 22);
            checkBoxTreeSize.Name = "checkBoxTreeSize";
            checkBoxTreeSize.Size = new System.Drawing.Size(104, 24);
            checkBoxTreeSize.TabIndex = 0;
            checkBoxTreeSize.Text = "TreeSize";
            checkBoxTreeSize.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(checkBoxMyAsus);
            groupBox1.Controls.Add(checkBoxHpSmart);
            groupBox1.Controls.Add(checkBoxLenovoVantage);
            groupBox1.Controls.Add(checkBoxSublimeText);
            groupBox1.Controls.Add(checkBox7ZIP);
            groupBox1.Controls.Add(checkBoxTeams);
            groupBox1.Controls.Add(checkBoxVLC);
            groupBox1.Controls.Add(checkBoxAdobe);
            groupBox1.Controls.Add(checkBoxLibreOffice);
            groupBox1.Controls.Add(checkBoxChrome);
            groupBox1.Location = new System.Drawing.Point(16, 74);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new System.Drawing.Size(316, 415);
            groupBox1.TabIndex = 11;
            groupBox1.TabStop = false;
            groupBox1.Text = "Setup de base";
            // 
            // checkBoxMyAsus
            // 
            checkBoxMyAsus.Location = new System.Drawing.Point(6, 216);
            checkBoxMyAsus.Name = "checkBoxMyAsus";
            checkBoxMyAsus.Size = new System.Drawing.Size(124, 24);
            checkBoxMyAsus.TabIndex = 10;
            checkBoxMyAsus.Text = "My Asus";
            checkBoxMyAsus.UseVisualStyleBackColor = true;
            // 
            // checkBoxHpSmart
            // 
            checkBoxHpSmart.Location = new System.Drawing.Point(186, 186);
            checkBoxHpSmart.Name = "checkBoxHpSmart";
            checkBoxHpSmart.Size = new System.Drawing.Size(124, 24);
            checkBoxHpSmart.TabIndex = 9;
            checkBoxHpSmart.Text = "HP Smart";
            checkBoxHpSmart.UseVisualStyleBackColor = true;
            // 
            // checkBoxLenovoVantage
            // 
            checkBoxLenovoVantage.Location = new System.Drawing.Point(6, 186);
            checkBoxLenovoVantage.Name = "checkBoxLenovoVantage";
            checkBoxLenovoVantage.Size = new System.Drawing.Size(124, 24);
            checkBoxLenovoVantage.TabIndex = 8;
            checkBoxLenovoVantage.Text = "Lenovo Vantage";
            checkBoxLenovoVantage.UseVisualStyleBackColor = true;
            // 
            // checkBoxSublimeText
            // 
            checkBoxSublimeText.Location = new System.Drawing.Point(6, 112);
            checkBoxSublimeText.Name = "checkBoxSublimeText";
            checkBoxSublimeText.Size = new System.Drawing.Size(104, 24);
            checkBoxSublimeText.TabIndex = 6;
            checkBoxSublimeText.Text = "Sublime Text";
            checkBoxSublimeText.UseVisualStyleBackColor = true;
            // 
            // checkBox7ZIP
            // 
            checkBox7ZIP.Location = new System.Drawing.Point(147, 82);
            checkBox7ZIP.Name = "checkBox7ZIP";
            checkBox7ZIP.Size = new System.Drawing.Size(104, 24);
            checkBox7ZIP.TabIndex = 5;
            checkBox7ZIP.Text = "7ZIP";
            checkBox7ZIP.UseVisualStyleBackColor = true;
            // 
            // checkBoxTeams
            // 
            checkBoxTeams.Location = new System.Drawing.Point(6, 82);
            checkBoxTeams.Name = "checkBoxTeams";
            checkBoxTeams.Size = new System.Drawing.Size(104, 24);
            checkBoxTeams.TabIndex = 5;
            checkBoxTeams.Text = "Teams";
            checkBoxTeams.UseVisualStyleBackColor = true;
            // 
            // checkBoxVLC
            // 
            checkBoxVLC.AccessibleName = "checkBoxVLC";
            checkBoxVLC.Location = new System.Drawing.Point(6, 22);
            checkBoxVLC.Name = "checkBoxVLC";
            checkBoxVLC.Size = new System.Drawing.Size(104, 24);
            checkBoxVLC.TabIndex = 1;
            checkBoxVLC.Text = "VLC";
            checkBoxVLC.UseVisualStyleBackColor = true;
            // 
            // checkBoxAdobe
            // 
            checkBoxAdobe.AccessibleName = "checkBoxAdobe";
            checkBoxAdobe.Location = new System.Drawing.Point(6, 52);
            checkBoxAdobe.Name = "checkBoxAdobe";
            checkBoxAdobe.Size = new System.Drawing.Size(104, 24);
            checkBoxAdobe.TabIndex = 2;
            checkBoxAdobe.Text = "Adobe";
            checkBoxAdobe.UseVisualStyleBackColor = true;
            // 
            // checkBoxLibreOffice
            // 
            checkBoxLibreOffice.Location = new System.Drawing.Point(147, 22);
            checkBoxLibreOffice.Name = "checkBoxLibreOffice";
            checkBoxLibreOffice.Size = new System.Drawing.Size(104, 24);
            checkBoxLibreOffice.TabIndex = 3;
            checkBoxLibreOffice.Text = "Libre Office";
            checkBoxLibreOffice.UseVisualStyleBackColor = true;
            // 
            // checkBoxChrome
            // 
            checkBoxChrome.Location = new System.Drawing.Point(147, 52);
            checkBoxChrome.Name = "checkBoxChrome";
            checkBoxChrome.Size = new System.Drawing.Size(104, 24);
            checkBoxChrome.TabIndex = 4;
            checkBoxChrome.Text = "Chrome";
            checkBoxChrome.UseVisualStyleBackColor = true;
            // 
            // InstallSelection
            // 
            InstallSelection.Location = new System.Drawing.Point(16, 19);
            InstallSelection.Name = "InstallSelection";
            InstallSelection.Size = new System.Drawing.Size(202, 31);
            InstallSelection.TabIndex = 0;
            InstallSelection.Text = "Installer la selection";
            InstallSelection.UseVisualStyleBackColor = true;
            InstallSelection.Click += BtnOpenPowerShell_Click;
            // 
            // tabOptimisation
            // 
            tabOptimisation.Controls.Add(groupBox7);
            tabOptimisation.Controls.Add(groupBox6);
            tabOptimisation.Location = new System.Drawing.Point(4, 28);
            tabOptimisation.Name = "tabOptimisation";
            tabOptimisation.Padding = new System.Windows.Forms.Padding(3);
            tabOptimisation.Size = new System.Drawing.Size(889, 576);
            tabOptimisation.TabIndex = 3;
            tabOptimisation.Text = "Optimisation";
            tabOptimisation.UseVisualStyleBackColor = true;
            // 
            // groupBox7
            // 
            groupBox7.Controls.Add(checkedListBoxServices);
            groupBox7.Controls.Add(buttonOptimisation);
            groupBox7.Location = new System.Drawing.Point(485, 15);
            groupBox7.Name = "groupBox7";
            groupBox7.Size = new System.Drawing.Size(396, 549);
            groupBox7.TabIndex = 3;
            groupBox7.TabStop = false;
            groupBox7.Text = "Services";
            // 
            // checkedListBoxServices
            // 
            checkedListBoxServices.FormattingEnabled = true;
            checkedListBoxServices.Location = new System.Drawing.Point(6, 56);
            checkedListBoxServices.Name = "checkedListBoxServices";
            checkedListBoxServices.Size = new System.Drawing.Size(384, 466);
            checkedListBoxServices.TabIndex = 2;
            // 
            // buttonOptimisation
            // 
            buttonOptimisation.Location = new System.Drawing.Point(121, 22);
            buttonOptimisation.Name = "buttonOptimisation";
            buttonOptimisation.Size = new System.Drawing.Size(166, 28);
            buttonOptimisation.TabIndex = 1;
            buttonOptimisation.Text = "Optimisation des services";
            buttonOptimisation.UseVisualStyleBackColor = true;
            buttonOptimisation.Click += BtnDisableSelectedServices_Click;
            // 
            // groupBox6
            // 
            groupBox6.Controls.Add(checkedListBoxApps);
            groupBox6.Controls.Add(buttonBloatWare);
            groupBox6.Location = new System.Drawing.Point(12, 15);
            groupBox6.Name = "groupBox6";
            groupBox6.Size = new System.Drawing.Size(396, 549);
            groupBox6.TabIndex = 2;
            groupBox6.TabStop = false;
            groupBox6.Text = "Bloatwares";
            // 
            // checkedListBoxApps
            // 
            checkedListBoxApps.FormattingEnabled = true;
            checkedListBoxApps.Location = new System.Drawing.Point(6, 56);
            checkedListBoxApps.Name = "checkedListBoxApps";
            checkedListBoxApps.Size = new System.Drawing.Size(384, 466);
            checkedListBoxApps.TabIndex = 1;
            // 
            // buttonBloatWare
            // 
            buttonBloatWare.Location = new System.Drawing.Point(114, 22);
            buttonBloatWare.Name = "buttonBloatWare";
            buttonBloatWare.Size = new System.Drawing.Size(166, 28);
            buttonBloatWare.TabIndex = 0;
            buttonBloatWare.Text = "Remove Bloatwares";
            buttonBloatWare.UseVisualStyleBackColor = true;
            buttonBloatWare.Click += BtnUninstallSelectedApps_Click;
            // 
            // tabVirus
            // 
            tabVirus.Controls.Add(btnCleanup);
            tabVirus.Controls.Add(checkedListBoxCleanupTools);
            tabVirus.Location = new System.Drawing.Point(4, 28);
            tabVirus.Name = "tabVirus";
            tabVirus.Padding = new System.Windows.Forms.Padding(3);
            tabVirus.Size = new System.Drawing.Size(889, 576);
            tabVirus.TabIndex = 4;
            tabVirus.Text = "Virus";
            tabVirus.UseVisualStyleBackColor = true;
            // 
            // checkedListBoxCleanupTools
            // 
            checkedListBoxCleanupTools.FormattingEnabled = true;
            checkedListBoxCleanupTools.Location = new System.Drawing.Point(20, 60);
            checkedListBoxCleanupTools.Name = "checkedListBoxCleanupTools";
            checkedListBoxCleanupTools.Size = new System.Drawing.Size(850, 480);
            checkedListBoxCleanupTools.TabIndex = 1;
            // 
            // btnCleanup
            // 
            btnCleanup.Location = new System.Drawing.Point(350, 20);
            btnCleanup.Name = "btnCleanup";
            btnCleanup.Size = new System.Drawing.Size(200, 30);
            btnCleanup.TabIndex = 0;
            btnCleanup.Text = "🧹 Nettoyer";
            btnCleanup.UseVisualStyleBackColor = true;
            btnCleanup.Click += BtnCleanup_Click;
            // 
            // labelCpuTemp
            // 
            labelCpuTemp.AccessibleName = "labelCpuTemp";
            labelCpuTemp.Location = new System.Drawing.Point(16, 510);
            labelCpuTemp.Name = "labelCpuTemp";
            labelCpuTemp.Size = new System.Drawing.Size(100, 16);
            labelCpuTemp.TabIndex = 1;
            labelCpuTemp.Text = "CPU : -- °C";
            // 
            // labelGpuTemp
            // 
            labelGpuTemp.AccessibleName = "labelGpuTemp";
            labelGpuTemp.Location = new System.Drawing.Point(16, 530);
            labelGpuTemp.Name = "labelGpuTemp";
            labelGpuTemp.Size = new System.Drawing.Size(100, 16);
            labelGpuTemp.TabIndex = 2;
            labelGpuTemp.Text = "GPU : -- °C";
            // 
            // labelWindowsActivation
            // 
            labelWindowsActivation.Location = new System.Drawing.Point(130, 510);
            labelWindowsActivation.Name = "labelWindowsActivation";
            labelWindowsActivation.Size = new System.Drawing.Size(176, 32);
            labelWindowsActivation.TabIndex = 0;
            labelWindowsActivation.Text = "Windows :";
            labelWindowsActivation.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // _buttonActivation
            // 
            _buttonActivation.Location = new System.Drawing.Point(130, 540);
            _buttonActivation.Name = "_buttonActivation";
            _buttonActivation.Size = new System.Drawing.Size(120, 30);
            _buttonActivation.TabIndex = 5;
            _buttonActivation.Text = "Activer";
            _buttonActivation.UseVisualStyleBackColor = true;
            _buttonActivation.Click += BtnActivateWindows_Click;
            // 
            // MainForm
            // 
            ClientSize = new System.Drawing.Size(897, 649);
            Controls.Add(AllTabs);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.Fixed3D;
            Icon = ((System.Drawing.Icon)resources.GetObject("$this.Icon"));
            MaximizeBox = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            Text = "Savior";
            Load += MainForm_Load;
            AllTabs.ResumeLayout(false);
            TabGeneral.ResumeLayout(false);
            groupBox11.ResumeLayout(false);
            groupBox5.ResumeLayout(false);
            TabSoftwares.ResumeLayout(false);
            groupBox4.ResumeLayout(false);
            groupBox3.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox1.ResumeLayout(false);
            tabOptimisation.ResumeLayout(false);
            groupBox7.ResumeLayout(false);
            groupBox6.ResumeLayout(false);
            tabVirus.ResumeLayout(false);
            ResumeLayout(false);
        }

        private System.Windows.Forms.TabPage tabVirus;

        // NOTE: removed previously added fields btnVirusClean and txtVirusLog to restore original file state

        private System.Windows.Forms.Button buttonGamingInstallGeneral;
        private System.Windows.Forms.Button NvidiaInstall;
        private System.Windows.Forms.Button AMDInstall;

        private System.Windows.Forms.Button btnInstallGpuSuite;

        private System.Windows.Forms.Button buttonMaj;
        private System.Windows.Forms.GroupBox groupBox11;

        private System.Windows.Forms.Button buttonBasicInstallGeneral;
        private GroupBox groupBox6;
        private GroupBox groupBox7;
        private System.Windows.Forms.TabPage tabOptimisation;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button InstallSelection;
        private System.Windows.Forms.TabControl AllTabs;
        private System.Windows.Forms.TabPage TabGeneral;
        private TabPage TabSoftwares;
        
    }
}

