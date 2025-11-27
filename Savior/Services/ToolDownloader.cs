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
    }
}
