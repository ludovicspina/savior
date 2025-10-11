// File: Services/WingetInstaller.cs

using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Savior.Services
{
    public static class WingetInstaller
    {
        public static async Task EnsureWingetHealthyAsync(Action<string>? log = null)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory; // dossier de Savior.exe
            string bundlePath = Path.Combine(baseDir, "Data", "Microsoft.DesktopAppInstaller_8wekyb3d8bbwe.msixbundle");

            // 0) si winget marche déjà, ne touche à rien
            // if (await WingetOkAsync())
            // {
            //     log?.Invoke("winget OK");
            //     return;
            // }

            if (!File.Exists(bundlePath))
                throw new FileNotFoundException("App Installer .msixbundle introuvable", bundlePath);

            log?.Invoke("Réparation App Installer (msixbundle)...");
            // 1) repair/upgrade in-place
            RunAdminPS($"""
                            Add-AppxPackage -Path '{bundlePath}' -ForceApplicationShutdown -ForceUpdateFromAnyVersion
                        """);

            // 2) sources
            log?.Invoke("Reset/update des sources winget...");
            RunAdminPS("winget source reset --force; winget source update");

            // 3) recheck
            if (!await WingetOkAsync())
                throw new InvalidOperationException("winget reste indisponible après réparation");

            log?.Invoke("winget opérationnel.");
        }

        private static void RunAdminPS(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                UseShellExecute = true,
                Verb = "runas"
            };
            using var p = Process.Start(psi);
            p?.WaitForExit();
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

        // ========= BOOTSTRAP =========
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

        // ========= VERSION "CONSOLE VISIBLE" (comme avant) =========
        // selection: logicalName -> checked ; ids map dans ce flux simple: logicalName == id si tu veux
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

            // mapping simple logicalName -> id (ici on considère que logicalName == id si tu passes déjà des IDs)
            foreach (var kv in selection)
            {
                if (kv.Value) // checked
                {
                    // si tu utilises un catalog JSON ailleurs, remplace kv.Key par l'ID réel
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
                        _ => kv.Key // fallback: déjà un ID
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
            // reset/update sources silencieux
            await ProcessRunner.RunHiddenAsync("winget", "source reset --force", s => log?.Invoke(s));
            if (token.IsCancellationRequested) return;
            await ProcessRunner.RunHiddenAsync("winget", "source update", s => log?.Invoke(s));
            if (token.IsCancellationRequested) return;

            foreach (var id in ids)
            {
                if (token.IsCancellationRequested) break;

                log?.Invoke($"--- Installing {id} ---");
                var args = $"install --id {id} -e --silent --accept-package-agreements --accept-source-agreements";
                var code = await ProcessRunner.RunHiddenAsync("winget", args, s => log?.Invoke(s));
                if (code != 0 && !token.IsCancellationRequested)
                {
                    log?.Invoke($"[WARN] {id} -> échec (code {code}). Reset sources + retry…");
                    await ProcessRunner.RunHiddenAsync("winget", "source reset --force", s => log?.Invoke(s));
                    await ProcessRunner.RunHiddenAsync("winget", "source update", s => log?.Invoke(s));
                    code = await ProcessRunner.RunHiddenAsync("winget", args, s => log?.Invoke(s));
                    if (code != 0) log?.Invoke($"[ERROR] {id} -> échec final (code {code})");
                    else log?.Invoke($"{id} -> OK après retry");
                }
                else
                {
                    log?.Invoke($"{id} -> OK");
                }

                step?.Invoke();
            }
        }
    }
    
}