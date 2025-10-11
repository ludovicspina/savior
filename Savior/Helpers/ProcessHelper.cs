using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Savior.Helpers
{
    public static class ProcessHelper
    {
        /// <summary>
        /// Lance un exécutable, avec ou sans élévation, et attend la fin.
        /// Retourne (ExitCode, Stdout+Stderr).
        /// </summary>
        public static async Task<(int exitCode, string output)> RunAsync(
            string exePath,
            string args,
            bool runAsAdmin = false,
            int timeoutMs = 60_000)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            if (runAsAdmin)
            {
                // Important : quand UseShellExecute=false, Verb=runas ne marche pas.
                // Donc si tu veux du vrai runas, il faut UseShellExecute=true → mais pas de redirection possible.
                psi.UseShellExecute = true;
                psi.Verb = "runas";
                psi.RedirectStandardOutput = false;
                psi.RedirectStandardError = false;
            }

            using var process = new Process { StartInfo = psi };
            var output = string.Empty;

            try
            {
                process.Start();

                if (!runAsAdmin)
                {
                    // Capture Stdout + Stderr seulement si pas en runas
                    output = await process.StandardOutput.ReadToEndAsync()
                           + await process.StandardError.ReadToEndAsync();
                }

                if (await Task.WhenAny(process.WaitForExitAsync(), Task.Delay(timeoutMs)) != process.WaitForExitAsync())
                {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException($"{exePath} a dépassé le timeout {timeoutMs}ms");
                }

                return (process.ExitCode, output);
            }
            catch (Exception ex)
            {
                return (-1, $"Erreur : {ex.Message}");
            }
        }
    }
}
