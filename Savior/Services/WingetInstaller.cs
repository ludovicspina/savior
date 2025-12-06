using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Savior.Services
{
    public static class WingetInstaller
    {
        public static async Task EnsureWingetHealthyAsync(Action<string>? log = null)
        {
            // First, check if winget is already working
            if (await WingetOkAsync())
            {
                log?.Invoke("✓ winget est déjà opérationnel.");
                return;
            }

            log?.Invoke("⚠️ winget n'est pas opérationnel, tentative de réparation...");

            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string bundlePath = Path.Combine(baseDir, "Data", "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle");

            // Check if local bundle exists, if not download it
            if (!File.Exists(bundlePath))
            {
                log?.Invoke("⚠️ Fichier .msixbundle local introuvable.");
                
                try
                {
                    // Download latest version from GitHub
                    bundlePath = await ToolDownloader.DownloadWingetAsync(s => log?.Invoke(s));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Impossible de télécharger winget depuis GitHub: {ex.Message}. " +
                        "Vérifiez votre connexion internet.", ex);
                }
            }
            else
            {
                log?.Invoke($"✓ Fichier .msixbundle trouvé: {Path.GetFileName(bundlePath)}");
            }

            log?.Invoke("⚙️ Installation/Réparation App Installer (msixbundle)...");
            
            // 1) repair/upgrade in-place via PowerShell hidden
            await ProcessRunner.RunHiddenAsync("powershell.exe", 
                $"-NoProfile -ExecutionPolicy Bypass -Command \"Add-AppxPackage -Path '{bundlePath}' -ForceApplicationShutdown -ForceUpdateFromAnyVersion\"",
                s => CleanLog(s, log));

            // 2) sources
            string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);
            log?.Invoke("🔄 Reset/update des sources winget...");
            await ProcessRunner.RunHiddenAsync("winget", "source reset --force", s => CleanLog(s, log), sys32);
            await ProcessRunner.RunHiddenAsync("winget", "source update", s => CleanLog(s, log), sys32);

            // 3) recheck
            if (!await WingetOkAsync())
                throw new InvalidOperationException("winget reste indisponible après réparation");

            log?.Invoke("✅ winget opérationnel.");
        }

        private static async Task<bool> WingetOkAsync()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var p = Process.Start(psi)!;
                string outp = await p.StandardOutput.ReadToEndAsync();
                await p.WaitForExitAsync();
                return p.ExitCode == 0 && !string.IsNullOrWhiteSpace(outp);
            }
            catch
            {
                return false;
            }
        }

        // ========= BOOTSTRAP (Legacy / Fallback) =========
        private static string BuildBootstrapScript()
        {
            return @"
$ErrorActionPreference = 'Continue'
Write-Host '== Vérification de winget =='

$wingetCmd = Get-Command winget -ErrorAction SilentlyContinue
if (-not $wingetCmd) {
  Write-Warning ""winget introuvable. Tentative de réenregistrement d'App Installer...""
  try {
    Add-AppxPackage -RegisterByFamilyName -MainPackage Microsoft.DesktopAppInstaller_8wekyb3d8bbwe -ErrorAction Stop
    $wingetCmd = Get-Command winget -ErrorAction SilentlyContinue
  } catch {
    Write-Warning 'Réenregistrement App Installer impossible. Installe App Installer via Microsoft Store.'
  }
}

# PATH (session)
$winApps = Join-Path $env:LOCALAPPDATA 'Microsoft\WindowsApps'
if ($env:PATH -notmatch [regex]::Escape($winApps)) { $env:PATH = ""$env:PATH;$winApps"" }

# Purge caches (corrige installs qui skip en 1s sur Win11)
try {
  $pkgPath = Join-Path $env:LOCALAPPDATA 'Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalState'
  if (Test-Path $pkgPath) {
    Get-ChildItem $pkgPath -Recurse -Force -ErrorAction SilentlyContinue |
      Where-Object { $_.Name -match 'cache' -or $_.Name -match 'source' -or $_.Name -match 'sqlite' } |
      Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
  }
} catch {}

# Reset/update des sources
try { winget source reset --force; winget source update } catch {
  Write-Warning 'winget source reset/update a renvoyé une erreur (souvent bénin).'
}
";
        }

        private static void RunPsElevated(string script)
        {
            var temp = Path.Combine(Path.GetTempPath(), $"winget_{Guid.NewGuid()}.ps1");
            File.WriteAllText(temp, script, Encoding.UTF8);
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{temp}\"",
                UseShellExecute = true,
                Verb = "runas"
            });
        }

        // ========= VERSION "CONSOLE VISIBLE" (Legacy) =========
        public static async Task InstallSelectedAsync(Dictionary<string, bool> selection, Action<string>? log = null)
        {
            // 1) bootstrap
            RunPsElevated(BuildBootstrapScript());
            await Task.Delay(800);

            // 2) construire le script d'installation
            var sb = new StringBuilder();
            sb.AppendLine("$ErrorActionPreference = 'Continue'");
            sb.AppendLine("function Install-AppId([string]$id) {");
            sb.AppendLine("  Write-Host (\"--- Installing {0} ---\" -f $id)");
            sb.AppendLine(
                "  Start-Process winget -Wait -ArgumentList (\"install --id {0} -e --silent --accept-package-agreements --accept-source-agreements\" -f $id)");
            sb.AppendLine("  if ($LASTEXITCODE -ne 0) {");
            sb.AppendLine(
                "    Write-Warning (\"{0} -> échec (code {1}). Reset sources + retry...\" -f $id, $LASTEXITCODE)");
            sb.AppendLine("    winget source reset --force; winget source update");
            sb.AppendLine(
                "    Start-Process winget -Wait -ArgumentList (\"install --id {0} -e --silent --accept-package-agreements --accept-source-agreements\" -f $id)");
            sb.AppendLine(
                "    if ($LASTEXITCODE -ne 0) { Write-Error (\"{0} -> échec final (code {1})\" -f $id, $LASTEXITCODE) }");
            sb.AppendLine("    else { Write-Host (\"{0} -> OK après retry\" -f $id) }");
            sb.AppendLine("  } else { Write-Host (\"{0} -> OK\" -f $id) }");
            sb.AppendLine("}");

            foreach (var kv in selection)
            {
                if (kv.Value) // checked
                {
                    var id = kv.Key switch
                    {
                        "VLC" => "VideoLAN.VLC",
                        "7ZIP" => "7zip.7zip",
                        "Chrome" => "Google.Chrome",
                        "Adobe" => "Adobe.Acrobat.Reader.64-bit",
                        "LibreOffice" => "TheDocumentFoundation.LibreOffice",
                        "Discord" => "Discord.Discord",
                        "Teams" => "Microsoft.Teams",
                        "TreeSize" => "JAMSoftware.TreeSize",
                        "HDDS" => "JanosMathe.HardDiskSentinel",
                        "NvidiaApp" => "Nvidia.App",
                        "LenovoVantage" => "9WZDNCRFJ4MV",
                        "HpSmart" => "9WZDNCRFHWLH",
                        "MyAsus" => "9N7R5S6B0ZZH",
                        "Bitdefender" => "Bitdefender.Bitdefender",
                        "Steam" => "Valve.Steam",
                        _ => kv.Key 
                    };
                    sb.AppendLine($"Install-AppId '{id}'");
                }
            }

            RunPsElevated(sb.ToString());
            log?.Invoke("Installations lancées (console PowerShell).");
        }

        // ========= VERSION "SILENCIEUSE" + PROGRESS =========
        public static async Task InstallSelectedWithProgressAsync(
            List<string> ids,
            Action<string>? log,
            Action? step,
            CancellationToken token)
        {
            string sys32 = Environment.GetFolderPath(Environment.SpecialFolder.System);

            // reset/update sources silencieux
            await ProcessRunner.RunHiddenAsync("winget", "source reset --force", s => CleanLog(s, log), sys32);
            if (token.IsCancellationRequested) return;
            await ProcessRunner.RunHiddenAsync("winget", "source update", s => CleanLog(s, log), sys32);
            if (token.IsCancellationRequested) return;

            foreach (var id in ids)
            {
                if (token.IsCancellationRequested) break;

                CleanLog($"--- Installing {id} ---", log);
                
                // Determine source: standard apps (with dots) -> winget, store apps (no dots) -> msstore
                string source = id.Contains('.') ? "winget" : "msstore";
                var args = $"install --id {id} -e --silent --accept-package-agreements --accept-source-agreements --source {source}";
                var code = await ProcessRunner.RunHiddenAsync("winget", args, s => CleanLog(s, log), sys32);

                // Check for specific exit codes
                // 0 = Success
                // -1978335189 (0x8A15002B) = No update available (already installed)
                if (code == 0)
                {
                    CleanLog($"{id} -> OK", log);
                }
                else if (code == -1978335189)
                {
                    CleanLog($"{id} -> Déjà à jour (ou plus récent).", log);
                }
                else if (!token.IsCancellationRequested)
                {
                    CleanLog($"[WARN] {id} -> échec (code {code}). Reset sources + retry…", log);
                    await ProcessRunner.RunHiddenAsync("winget", "source reset --force", s => CleanLog(s, log), sys32);
                    await ProcessRunner.RunHiddenAsync("winget", "source update", s => CleanLog(s, log), sys32);
                    code = await ProcessRunner.RunHiddenAsync("winget", args, s => CleanLog(s, log), sys32);
                    
                    if (code == 0) CleanLog($"{id} -> OK après retry", log);
                    else if (code == -1978335189) CleanLog($"{id} -> Déjà à jour après retry.", log);
                    else CleanLog($"[ERROR] {id} -> échec final (code {code})", log);
                }

                step?.Invoke();
            }
        }

        private static void CleanLog(string? line, Action<string>? log)
        {
            if (string.IsNullOrWhiteSpace(line)) return;
            
            // Filter common noise
            if (line.Contains("nécessite des privilèges d'administrateur")) return;
            if (line.Contains("privilèges d'administrateur")) return;
            if (line.Contains("Mise à jour en cours de toutes les sources")) return;
            if (line.Contains("Terminé")) return; // Often redundant
            
            // Filter progress bars
            if (line.Contains("█") || line.Contains("▒")) return;
            if (line.Trim() == "-") return;
            if (line.Trim() == "\\") return;
            if (line.Trim() == "|") return;
            if (line.Trim() == "/") return;
            if (line.Trim().All(c => c == '-' || c == ' ')) return; // Progress bars like "   -   "
            
            // Simplify "Mise à jour..."
            if (line.Contains("Mise à jour de la source"))
            {
                 var parts = line.Split(':');
                 if (parts.Length > 1) log?.Invoke($"Update source: {parts[1].Trim()}");
                 else log?.Invoke(line.Trim());
                 return;
            }

            log?.Invoke(line.Trim());
        }
    }
}