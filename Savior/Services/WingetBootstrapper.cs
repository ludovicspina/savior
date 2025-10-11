using System.Diagnostics;

namespace Savior.Services;

public static class WingetBootstrapper
{
    // Retourne un script PS qui : (1) répare App Installer si besoin, (2) fixe PATH,
    // (3) purge cache LocalState (Windows 11), (4) reset/update sources.
    public static string BuildBootstrapScript()
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

    public static void RunElevated(string scriptContent)
    {
        var temp = Path.Combine(Path.GetTempPath(), $"winget_bootstrap_{Guid.NewGuid()}.ps1");
        File.WriteAllText(temp, scriptContent);
        Process.Start(new ProcessStartInfo {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{temp}\"",
            UseShellExecute = true,
            Verb = "runas"
        });
    }
}
