namespace Savior.Constants
{
    public static class CleanupTools
    {
        public static readonly List<CleanupTool> All = new()
        {
            // 🧹 Nettoyage de Base
            new CleanupTool("Nettoyage de Disque Windows", CleanupCategory.Basic, 
                "Supprime les fichiers temporaires, cache, corbeille via l'outil Windows",
                "cleanmgr.exe /sagerun:1"),
            
            new CleanupTool("Fichiers Temporaires", CleanupCategory.Basic,
                "Supprime manuellement les fichiers temporaires de Windows et utilisateur",
                @"
                function Clean-Folder($path, $name) {
                    if (Test-Path $path) {
                        $files = Get-ChildItem -Path $path -Recurse -Force -ErrorAction SilentlyContinue
                        $count = ($files | Measure-Object).Count
                        $size = ($files | Measure-Object -Property Length -Sum).Sum / 1MB
                        
                        if ($count -gt 0) {
                            Remove-Item -Path ""$path\*"" -Recurse -Force -ErrorAction SilentlyContinue
                            Write-Output ""✓ $name : $count fichiers supprimés ($([math]::Round($size, 2)) MB)""
                        } else {
                            Write-Output ""- $name : Déjà vide""
                        }
                    }
                }

                Clean-Folder $env:TEMP ""Temp Utilisateur""
                Clean-Folder ""C:\Windows\Temp"" ""Temp Windows""
                Clean-Folder ""C:\Windows\Prefetch"" ""Prefetch""
                "),
            
            new CleanupTool("Vider la Corbeille", CleanupCategory.Basic,
                "Vide la corbeille de tous les lecteurs",
                "Clear-RecycleBin -Force -ErrorAction SilentlyContinue; Write-Output 'Corbeille vidée'"),

            // 🔧 Réparation Système
            new CleanupTool("SFC - Vérification Système", CleanupCategory.SystemRepair,
                "Scanne et répare les fichiers système corrompus (peut prendre 15-30 min)",
                "sfc /scannow"),
            
            new CleanupTool("DISM - Réparation Image", CleanupCategory.SystemRepair,
                "Répare l'image Windows (peut prendre 15-30 min)",
                "DISM /Online /Cleanup-Image /RestoreHealth"),

            // 🌐 Nettoyage Réseau & Cache
            new CleanupTool("Cache DNS", CleanupCategory.NetworkCache,
                "Vide le cache DNS",
                "ipconfig /flushdns"),
            
            new CleanupTool("Cache Windows Store", CleanupCategory.NetworkCache,
                "Réinitialise le cache du Windows Store",
                "wsreset.exe"),

            // 📦 Optimisations Avancées
            new CleanupTool("Windows Update Cleanup", CleanupCategory.Advanced,
                "Supprime les anciens fichiers de mise à jour Windows",
                "DISM /Online /Cleanup-Image /StartComponentCleanup /ResetBase"),
            
            new CleanupTool("Logs Windows Anciens", CleanupCategory.Advanced,
                "Supprime les fichiers logs de plus de 30 jours",
                @"
                Get-ChildItem -Path C:\Windows\Logs -Include *.log -Recurse -ErrorAction SilentlyContinue | 
                Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | 
                Remove-Item -Force -ErrorAction SilentlyContinue
                Write-Output 'Logs anciens supprimés'
                "),
            
            new CleanupTool("Optimisation de Disque", CleanupCategory.Advanced,
                "Optimise le disque C: (TRIM pour SSD, Défragmentation pour HDD)",
                "Optimize-Volume -DriveLetter C -ReTrim -Verbose"),

            // 🛡️ Sécurité & Anti-Malware
            new CleanupTool("Windows Defender - Scan Complet", CleanupCategory.SecurityScan,
                "Lance un scan antivirus complet avec Windows Defender (peut prendre 30-60 min)",
                @"
                Write-Output 'Démarrage du scan Windows Defender...'
                Start-MpScan -ScanType FullScan
                Write-Output 'Scan Windows Defender terminé'
                "),

            new CleanupTool("AdwCleaner - Suppression Adware", CleanupCategory.SecurityScan,
                "Télécharge et lance AdwCleaner (vous devrez lancer le scan et nettoyer manuellement)",
                "DOWNLOAD_ADWCLEANER"), // Special command handled by RunCleanupAsync

            new CleanupTool("RKill - Tuer Processus Malveillants", CleanupCategory.SecurityScan,
                "⚠️ ATTENTION: Fermera Savior! À exécuter MANUELLEMENT depuis Tools/rkill.exe AVANT de lancer les autres scans",
                "LOCAL_TOOL:rkill.exe"),

            new CleanupTool("Autoruns - Analyse Démarrage", CleanupCategory.SecurityScan,
                "Outil Microsoft Sysinternals - Liste tout ce qui démarre avec Windows (très puissant)",
                "LOCAL_TOOL:Autoruns64.exe"),

            new CleanupTool("FRST - Diagnostic Système", CleanupCategory.SecurityScan,
                "Farbar Recovery Scan Tool - Génère des logs détaillés pour diagnostic approfondi",
                "LOCAL_TOOL:FRST64.exe"),

            new CleanupTool("HijackThis - Détection Détournement", CleanupCategory.SecurityScan,
                "Analyse les points d'entrée système pour détecter les détournements de navigateur",
                "LOCAL_TOOL:HijackThis.exe"),

            new CleanupTool("Réinitialisation Navigateurs", CleanupCategory.SecurityScan,
                "Réinitialise Chrome, Edge et Firefox (supprime extensions, cache, désactive notifications)",
                @"
                Write-Output '=== Réinitialisation des navigateurs ==='
                
                # Chrome
                $chromePath = ""$env:LOCALAPPDATA\Google\Chrome\User Data\Default""
                if (Test-Path $chromePath) {
                    Write-Output '--- Chrome ---'
                    Stop-Process -Name chrome -Force -ErrorAction SilentlyContinue
                    Start-Sleep -Seconds 2
                    
                    # Extensions
                    $extPath = ""$chromePath\Extensions""
                    if (Test-Path $extPath) {
                        $exts = Get-ChildItem $extPath -Directory
                        foreach ($ext in $exts) {
                            Write-Output ""Suppression extension : $($ext.Name)""
                            Remove-Item $ext.FullName -Recurse -Force -ErrorAction SilentlyContinue
                        }
                    }
                    
                    # Cache
                    $cachePath = ""$chromePath\Cache""
                    if (Test-Path $cachePath) {
                        $size = (Get-ChildItem $cachePath -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
                        Remove-Item $cachePath -Recurse -Force -ErrorAction SilentlyContinue
                        Write-Output ""Cache vidé : $([math]::Round($size, 2)) MB""
                    }

                    # Disable notifications
                    $prefsFile = ""$chromePath\Preferences""
                    if (Test-Path $prefsFile) {
                        $prefs = Get-Content $prefsFile -Raw | ConvertFrom-Json
                        if (-not $prefs.profile.default_content_setting_values) {
                            $prefs.profile | Add-Member -NotePropertyName 'default_content_setting_values' -NotePropertyValue @{} -Force
                        }
                        $prefs.profile.default_content_setting_values | Add-Member -NotePropertyName 'notifications' -NotePropertyValue 2 -Force
                        $prefs | ConvertTo-Json -Depth 100 | Set-Content $prefsFile -Encoding UTF8
                        Write-Output 'Notifications désactivées'
                    }
                }
                
                # Edge
                $edgePath = ""$env:LOCALAPPDATA\Microsoft\Edge\User Data\Default""
                if (Test-Path $edgePath) {
                    Write-Output '--- Edge ---'
                    Stop-Process -Name msedge -Force -ErrorAction SilentlyContinue
                    Start-Sleep -Seconds 2
                    
                    # Extensions
                    $extPath = ""$edgePath\Extensions""
                    if (Test-Path $extPath) {
                        $exts = Get-ChildItem $extPath -Directory
                        foreach ($ext in $exts) {
                            Write-Output ""Suppression extension : $($ext.Name)""
                            Remove-Item $ext.FullName -Recurse -Force -ErrorAction SilentlyContinue
                        }
                    }

                    # Cache
                    $cachePath = ""$edgePath\Cache""
                    if (Test-Path $cachePath) {
                        $size = (Get-ChildItem $cachePath -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
                        Remove-Item $cachePath -Recurse -Force -ErrorAction SilentlyContinue
                        Write-Output ""Cache vidé : $([math]::Round($size, 2)) MB""
                    }
                    
                    # Disable notifications
                    $prefsFile = ""$edgePath\Preferences""
                    if (Test-Path $prefsFile) {
                        $prefs = Get-Content $prefsFile -Raw | ConvertFrom-Json
                        if (-not $prefs.profile.default_content_setting_values) {
                            $prefs.profile | Add-Member -NotePropertyName 'default_content_setting_values' -NotePropertyValue @{} -Force
                        }
                        $prefs.profile.default_content_setting_values | Add-Member -NotePropertyName 'notifications' -NotePropertyValue 2 -Force
                        $prefs | ConvertTo-Json -Depth 100 | Set-Content $prefsFile -Encoding UTF8
                        Write-Output 'Notifications désactivées'
                    }
                }
                
                # Firefox
                $firefoxPath = ""$env:APPDATA\Mozilla\Firefox\Profiles""
                if (Test-Path $firefoxPath) {
                    Write-Output '--- Firefox ---'
                    Stop-Process -Name firefox -Force -ErrorAction SilentlyContinue
                    Start-Sleep -Seconds 2
                    Get-ChildItem -Path $firefoxPath -Directory | ForEach-Object {
                        $profileName = $_.Name
                        
                        # Extensions
                        $extPath = ""$($_.FullName)\extensions""
                        if (Test-Path $extPath) {
                            $exts = Get-ChildItem $extPath
                            foreach ($ext in $exts) {
                                Write-Output ""[$profileName] Suppression extension : $($ext.Name)""
                                Remove-Item $ext.FullName -Recurse -Force -ErrorAction SilentlyContinue
                            }
                        }

                        # Cache
                        $cachePath = ""$($_.FullName)\cache2""
                        if (Test-Path $cachePath) {
                            $size = (Get-ChildItem $cachePath -Recurse -Force -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB
                            Remove-Item $cachePath -Recurse -Force -ErrorAction SilentlyContinue
                            Write-Output ""[$profileName] Cache vidé : $([math]::Round($size, 2)) MB""
                        }
                        
                        # Disable notifications
                        $prefsFile = ""$($_.FullName)\prefs.js""
                        if (Test-Path $prefsFile) {
                            $content = Get-Content $prefsFile
                            $content = $content | Where-Object { $_ -notmatch 'permissions.default.desktop-notification' }
                            $content += 'user_pref(""permissions.default.desktop-notification"", 2);'
                            $content | Set-Content $prefsFile -Encoding UTF8
                            Write-Output ""[$profileName] Notifications désactivées""
                        }
                    }
                }
                
                Write-Output '✓ Navigateurs traités'
                ")
        };
    }

    public class CleanupTool
    {
        public string Name { get; }
        public CleanupCategory Category { get; }
        public string Description { get; }
        public string Command { get; }

        public CleanupTool(string name, CleanupCategory category, string description, string command)
        {
            Name = name;
            Category = category;
            Description = description;
            Command = command;
        }
    }

    public enum CleanupCategory
    {
        Basic,
        SystemRepair,
        NetworkCache,
        Advanced,
        SecurityScan
    }
}
