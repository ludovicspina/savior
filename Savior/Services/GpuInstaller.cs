using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Savior.Helpers;

namespace Savior.Services
{
    public static class GpuInstaller
    {
        // --- Config records ---
        private record NvidiaCfg(string? app_url, string? driver_url, string? silent_args, string? fallback_page);

        private record AmdCfg(string? adrenalin_url, string? silent_args, string? fallback_page);

        private record Cfg(NvidiaCfg nvidia, AmdCfg amd);

        private static string BaseDir => AppContext.BaseDirectory;

        // -------- Public orchestrators --------
        public static async Task InstallForDetectedGpuAsync(Action<string>? log = null)
        {
            var vendor = DetectGpuVendor();
            log?.Invoke($"GPU détecté : {vendor}");

            var cfg = LoadConfig();
            var temp = Path.GetTempPath();

            switch (vendor)
            {
                case GpuVendor.Nvidia:
                    await InstallNvidiaAppAsync(cfg.nvidia, temp, log);
                    break;

                case GpuVendor.Amd:
                    await InstallAmdAdrenalinAsync(cfg.amd, temp, log);
                    break;

                default:
                    log?.Invoke("⚠️ GPU ni NVIDIA ni AMD (ou non détecté) : rien à installer.");
                    break;
            }
        }

        public static async Task InstallNvidiaGpu(Action<string>? log = null)
        {
            await InstallNvidiaAppAsync(log);
        }
        
        public static async Task InstallAmdGpu(Action<string>? log = null)
        {
            await InstallAmdAdrenalinAsync(log);
        }

        // Option : appels directs par bouton dédié
        public static Task InstallNvidiaAppAsync(Action<string>? log = null)
            => InstallNvidiaAppAsync(LoadConfig().nvidia, Path.GetTempPath(), log);

        public static Task InstallAmdAdrenalinAsync(Action<string>? log = null)
            => InstallAmdAdrenalinAsync(LoadConfig().amd, Path.GetTempPath(), log);

        // -------- Vendor detection --------
        public enum GpuVendor
        {
            Nvidia,
            Amd,
            Unknown
        }

        public static GpuVendor DetectGpuVendor()
        {
            try
            {
                using var searcher =
                    new ManagementObjectSearcher("select Name, AdapterCompatibility from Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var name = (obj["Name"]?.ToString() ?? "").ToLowerInvariant();
                    var compat = (obj["AdapterCompatibility"]?.ToString() ?? "").ToLowerInvariant();
                    if (name.Contains("nvidia") || compat.Contains("nvidia")) return GpuVendor.Nvidia;
                    if (name.Contains("amd") || name.Contains("advanced micro devices") || compat.Contains("amd"))
                        return GpuVendor.Amd;
                }
            }
            catch
            {
                /* ignore */
            }

            return GpuVendor.Unknown;
        }

        // -------- NVIDIA --------
        private static async Task InstallNvidiaAppAsync(NvidiaCfg cfg, string downloadDir, Action<string>? log)
        {
            // Cherche le fichier local NVIDIA_app.exe
            var root = Path.Combine(BaseDir, "Data", "Installers");
            var local = Directory.GetFiles(root, "*.exe", SearchOption.AllDirectories)
                .FirstOrDefault(f => Path.GetFileName(f).Equals("NVIDIA_app.exe", StringComparison.OrdinalIgnoreCase));

            if (local != null)
            {
                log?.Invoke($"➡️ Lancement NVIDIA App local : {local}");

                var (exitCode, _) = await ProcessHelper.RunAsync(
                    local,
                    cfg.silent_args ?? "/S",
                    runAsAdmin: true,
                    timeoutMs: 30 * 60 * 1000
                );

                if (exitCode == 0 || exitCode == 3010)
                    log?.Invoke("✅ NVIDIA App installé avec succès.");
                else
                    log?.Invoke($"⚠️ NVIDIA App a échoué (exit {exitCode}).");
            }
            else
            {
                log?.Invoke("⚠️ Impossible de trouver NVIDIA_app.exe dans Data/Installers");
            }
        }


        private static async Task<string?> DownloadLatestNvidiaAppExeAsync(string downloadDir, Action<string>? log)
        {
            // On encapsule le PowerShell pour parser la page NVIDIA App ou Drivers
            var ps = new StringBuilder();
            ps.AppendLine("$ErrorActionPreference = 'Stop'");
            ps.AppendLine("[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12");
            ps.AppendLine("function Get-NvidiaAppLatestUrl {");
            ps.AppendLine("  param([string]$AppPage = 'https://www.nvidia.com/en-us/software/nvidia-app/')");
            ps.AppendLine("  $ProgressPreference = 'SilentlyContinue'");
            ps.AppendLine("  try {");
            ps.AppendLine("    $headers = @{ 'User-Agent' = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)' }");
            ps.AppendLine("    $resp = Invoke-WebRequest -Uri $AppPage -Headers $headers -TimeoutSec 20");
            ps.AppendLine("    $html = $resp.Content");
            ps.AppendLine(
                "    $re1 = 'https://us\\.download\\.nvidia\\.com/nvapp/client/[\\d\\.]+/NVIDIA_app_v[\\d\\.]+' + '.exe'");
            ps.AppendLine("    if ($html -match $re1) { return $matches[0] }");
            ps.AppendLine("    $re2 = 'href=\"([^\"]*nv[^\"]*app[^\"]*\\.exe)\"'");
            ps.AppendLine("    if ($html -match $re2) { return $matches[1] }");
            ps.AppendLine(
                "    $resp2 = Invoke-WebRequest -Uri 'https://www.nvidia.com/en-us/drivers/' -Headers $headers -TimeoutSec 20");
            ps.AppendLine("    $html2 = $resp2.Content");
            ps.AppendLine("    if ($html2 -match $re1) { return $matches[0] }");
            ps.AppendLine("    if ($html2 -match $re2) { return $matches[1] }");
            ps.AppendLine("  } catch {}");
            ps.AppendLine("  return $null");
            ps.AppendLine("}");
            ps.AppendLine("$url = Get-NvidiaAppLatestUrl");
            ps.AppendLine("if (-not $url) { throw 'NVIDIA App URL introuvable' }");
            ps.AppendLine($"$out = Join-Path '{downloadDir.Replace("'", "''")}' ([System.IO.Path]::GetFileName($url))");
            ps.AppendLine("Invoke-WebRequest -Uri $url -OutFile $out -UseBasicParsing");
            ps.AppendLine("$out");

            string? installerPath = await PowerShellScriptBuilder.RunAndGetStdoutAsync(ps.ToString());
            return string.IsNullOrWhiteSpace(installerPath) ? null : installerPath.Trim();
        }

        // -------- AMD --------
// Remplace entièrement InstallAmdAdrenalinAsync(AmdCfg cfg, string downloadDir, Action<string>? log)
        private static async Task InstallAmdAdrenalinAsync(AmdCfg cfg, string downloadDir, Action<string>? log)
        {
            var root = Path.Combine(BaseDir, "Data", "Installers");
            var local = Directory.Exists(root)
                ? Directory.GetFiles(root, "*.exe", SearchOption.AllDirectories)
                    .FirstOrDefault(f =>
                    {
                        var n = Path.GetFileName(f).ToLowerInvariant();
                        return n.Equals("amd_adrenalin.exe", StringComparison.OrdinalIgnoreCase)
                               || (n.Contains("amd") && (n.Contains("adrenalin") || n.Contains("radeon") ||
                                                         n.Contains("amd-software")));
                    })
                : null;

            if (local == null)
            {
                log?.Invoke("⚠️ AMD Adrenalin introuvable dans Data/Installers");
                return;
            }

            var silent = string.IsNullOrWhiteSpace(cfg.silent_args) ? "-install -quiet -norestart" : cfg.silent_args;

            log?.Invoke($"➡️ Lancement AMD Adrenalin : {local}");
            var (exit1, _) =
                await Savior.Helpers.ProcessHelper.RunAsync(local, silent, runAsAdmin: true, timeoutMs: 30 * 60 * 1000);
            if (exit1 == 0 || exit1 == 3010)
            {
                log?.Invoke("✅ AMD Adrenalin installé (peut demander un redémarrage).");
                return;
            }

            log?.Invoke("↪️ Relance en mode interactif…");
            var (exit2, _) =
                await Savior.Helpers.ProcessHelper.RunAsync(local, "", runAsAdmin: true, timeoutMs: 30 * 60 * 1000);
            if (exit2 == 0 || exit2 == 3010) log?.Invoke("✅ AMD Adrenalin installé (interactif).");
            else log?.Invoke($"❌ Échec AMD Adrenalin (exit {exit2}).");
        }

        private static async Task<string?> DownloadAmdAutoDetectAsync(string downloadDir, Action<string>? log)
        {
            var ps = @"
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$headers = @{ 'User-Agent' = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)' }
$resp = Invoke-WebRequest -Uri 'https://www.amd.com/en/support/download/drivers.html' -Headers $headers -TimeoutSec 20
$html = $resp.Content
$re = 'href=""([^""]*Auto-Detect[^""]*\.exe)""'
if ($html -match $re) { $url = $matches[1] } else { $url = $null }
if (-not $url) {
  $resp2 = Invoke-WebRequest -Uri 'https://www.amd.com/en/resources/support-articles/faqs/GPU-131.html' -Headers $headers -TimeoutSec 20
  $html2 = $resp2.Content
  if ($html2 -match $re) { $url = $matches[1] }
}
if (-not $url) { throw 'AMD Auto-Detect URL introuvable' }
$out = Join-Path '" + downloadDir.Replace("'", "''") + @"' ([System.IO.Path]::GetFileName($url))
Invoke-WebRequest -Uri $url -OutFile $out -UseBasicParsing
$out
";
            string? path = await PowerShellScriptBuilder.RunAndGetStdoutAsync(ps);
            return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        }

        // -------- Shared helpers --------
        private static Cfg LoadConfig()
        {
            var path = Path.Combine(BaseDir, "Config", "GPUInstallers.json");
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Cfg>(json)!;
        }

        private static string? FindLocalInstaller(string vendor, string[] keywords)
        {
            var root = Path.Combine(BaseDir, "Data", "Installers");
            if (!Directory.Exists(root)) return null;
            var files = Directory.GetFiles(root, "*.exe", SearchOption.AllDirectories);
            return files.FirstOrDefault(p =>
            {
                var lower = Path.GetFileName(p).ToLowerInvariant();
                return lower.Contains(vendor) && keywords.Any(k => lower.Contains(k));
            });
        }

        private static async Task<string?> DownloadToAsync(string url, string downloadDir, string prefix,
            Action<string>? log)
        {
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
                var bytes = await http.GetByteArrayAsync(url);
                Directory.CreateDirectory(downloadDir);
                var dest = Path.Combine(downloadDir, $"{prefix}_{Guid.NewGuid():N}.exe");
                await File.WriteAllBytesAsync(dest, bytes);
                log?.Invoke($"Téléchargé → {dest}");
                return dest;
            }
            catch (Exception ex)
            {
                log?.Invoke("DL error: " + ex.Message);
                return null;
            }
        }

        private static async Task<bool> TryRunSilentThenInteractive(string exePath, string silentArgs,
            Action<string>? log)
        {
            // 1) Tentative silencieuse
            var (exit1, _) =
                await ProcessHelper.RunAsync(exePath, silentArgs, runAsAdmin: true, timeoutMs: 30 * 60 * 1000);
            if (exit1 == 0 || exit1 == 3010) return true;

            // 2) Fallback interactif
            log?.Invoke("Relance en mode interactif…");
            var (exit2, _) = await ProcessHelper.RunAsync(exePath, "", runAsAdmin: true, timeoutMs: 30 * 60 * 1000);
            return exit2 == 0 || exit2 == 3010;
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            }
            catch
            {
                /* ignore */
            }
        }
    }
}