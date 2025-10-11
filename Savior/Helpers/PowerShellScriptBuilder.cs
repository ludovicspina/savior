using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Savior.Helpers
{
    public static class PowerShellScriptBuilder
    {
        /// <summary>
        /// Exécute un script PowerShell donné en texte et retourne la sortie standard.
        /// </summary>
        public static async Task<string?> RunAndGetStdoutAsync(string script, int timeoutMs = 60_000)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -Command -",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var output = new StringBuilder();
            var tcs = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.ErrorDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
            process.Exited += (s, e) => tcs.TrySetResult(true);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.StandardInput.WriteLineAsync(script);
            process.StandardInput.Close();

            // Timeout de sécurité
            var finished = await Task.WhenAny(tcs.Task, Task.Delay(timeoutMs));
            if (finished != tcs.Task)
            {
                try { process.Kill(); } catch { /* ignore */ }
                throw new TimeoutException("PowerShell script timed out");
            }

            return output.ToString().Trim();
        }
    }
}
