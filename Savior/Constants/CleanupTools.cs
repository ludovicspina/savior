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
                Remove-Item -Path $env:TEMP\* -Recurse -Force -ErrorAction SilentlyContinue
                Remove-Item -Path C:\Windows\Temp\* -Recurse -Force -ErrorAction SilentlyContinue
                Remove-Item -Path C:\Windows\Prefetch\* -Force -ErrorAction SilentlyContinue
                Write-Output 'Fichiers temporaires supprimés'
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
                "Optimize-Volume -DriveLetter C -ReTrim -Verbose")
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
        Advanced
    }
}
