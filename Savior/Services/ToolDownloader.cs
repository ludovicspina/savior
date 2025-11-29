using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Savior.Services
{
    public static class ToolDownloader
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>
        /// Downloads AdwCleaner from the official Malwarebytes CDN
        /// </summary>
        /// <param name="log">Action to log progress messages</param>
        /// <returns>Path to the downloaded executable</returns>
        public static async Task<string> DownloadAdwCleanerAsync(Action<string> log)
        {
            string downloadUrl = "https://downloads.malwarebytes.com/file/adwcleaner";
            string tempPath = Path.Combine(Path.GetTempPath(), "adwcleaner.exe");

            try
            {
                log("📥 Téléchargement d'AdwCleaner...");

                // Delete existing file if present
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                // Download the file
                var response = await _httpClient.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(tempPath, fileBytes);

                log($"✓ AdwCleaner téléchargé ({fileBytes.Length / 1024 / 1024:F1} MB)");
                return tempPath;
            }
            catch (Exception ex)
            {
                log($"[ERROR] Échec du téléchargement d'AdwCleaner: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Downloads FRST (Farbar Recovery Scan Tool) - 64-bit version
        /// </summary>
        /// <param name="log">Action to log progress messages</param>
        /// <returns>Path to the downloaded executable</returns>
        public static async Task<string> DownloadFRSTAsync(Action<string> log)
        {
            // Note: FRST doesn't have a direct download link, it requires manual download
            // This is a placeholder for future implementation if needed
            string downloadUrl = "https://download.bleepingcomputer.com/farbar/FRST64.exe";
            string tempPath = Path.Combine(Path.GetTempPath(), "FRST64.exe");

            try
            {
                log("📥 Téléchargement de FRST...");

                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }

                var response = await _httpClient.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(tempPath, fileBytes);

                log($"✓ FRST téléchargé ({fileBytes.Length / 1024 / 1024:F1} MB)");
                return tempPath;
            }
            catch (Exception ex)
            {
                log($"[ERROR] Échec du téléchargement de FRST: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Downloads the latest stable version of winget (Windows Package Manager) from GitHub
        /// </summary>
        /// <param name="log">Action to log progress messages</param>
        /// <returns>Path to the downloaded .msixbundle file</returns>
        public static async Task<string> DownloadWingetAsync(Action<string> log)
        {
            string apiUrl = "https://api.github.com/repos/microsoft/winget-cli/releases/latest";
            
            try
            {
                log("📥 Récupération des informations de la dernière version de winget...");

                // Configure HttpClient with User-Agent (required by GitHub API)
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "Savior-WingetInstaller");

                // Get latest release information from GitHub API
                var response = await _httpClient.GetAsync(apiUrl);
                response.EnsureSuccessStatusCode();

                var jsonContent = await response.Content.ReadAsStringAsync();
                
                // Parse JSON manually (simple approach without third-party library)
                string downloadUrl = null;
                string fileName = null;
                string version = null;

                // Extract tag_name
                int tagIndex = jsonContent.IndexOf("\"tag_name\":");
                if (tagIndex != -1)
                {
                    int startQuote = jsonContent.IndexOf("\"", tagIndex + 11);
                    int endQuote = jsonContent.IndexOf("\"", startQuote + 1);
                    version = jsonContent.Substring(startQuote + 1, endQuote - startQuote - 1);
                }

                // Find .msixbundle asset
                int assetsIndex = jsonContent.IndexOf("\"assets\":");
                if (assetsIndex != -1)
                {
                    string assetsSection = jsonContent.Substring(assetsIndex);
                    int msixIndex = assetsSection.IndexOf(".msixbundle");
                    
                    if (msixIndex != -1)
                    {
                        // Find browser_download_url before .msixbundle
                        int urlSearchStart = assetsSection.LastIndexOf("\"browser_download_url\":", msixIndex);
                        if (urlSearchStart != -1)
                        {
                            int urlStart = assetsSection.IndexOf("\"", urlSearchStart + 23);
                            int urlEnd = assetsSection.IndexOf("\"", urlStart + 1);
                            downloadUrl = assetsSection.Substring(urlStart + 1, urlEnd - urlStart - 1);
                            
                            // Extract filename from URL
                            fileName = downloadUrl.Substring(downloadUrl.LastIndexOf('/') + 1);
                        }
                    }
                }

                if (string.IsNullOrEmpty(downloadUrl) || string.IsNullOrEmpty(fileName))
                {
                    throw new Exception("Impossible de trouver le fichier .msixbundle dans la dernière version");
                }

                log($"✓ Version trouvée: {version ?? "Unknown"}");
                log($"📦 Fichier: {fileName}");

                // Create Data folder if it doesn't exist
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string dataFolder = Path.Combine(baseDir, "Data");
                if (!Directory.Exists(dataFolder))
                {
                    Directory.CreateDirectory(dataFolder);
                    log($"✓ Dossier Data créé");
                }

                string downloadPath = Path.Combine(dataFolder, fileName);

                // Download the file
                log($"📥 Téléchargement de winget... (cela peut prendre quelques minutes)");
                
                _httpClient.DefaultRequestHeaders.Clear();
                var downloadResponse = await _httpClient.GetAsync(downloadUrl);
                downloadResponse.EnsureSuccessStatusCode();

                var fileBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(downloadPath, fileBytes);

                log($"✓ Winget téléchargé ({fileBytes.Length / 1024 / 1024:F1} MB)");
                log($"✓ Sauvegardé dans: {downloadPath}");
                
                return downloadPath;
            }
            catch (Exception ex)
            {
                log($"[ERROR] Échec du téléchargement de winget: {ex.Message}");
                throw;
            }
        }
    }
}
