using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Savior.Services
{
    public static class ProcessRunner
    {
        public static async Task<int> RunHiddenAsync(string file, string args, Action<string>? onLine = null, string? workingDirectory = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                WorkingDirectory = workingDirectory ?? AppDomain.CurrentDomain.BaseDirectory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            };
            var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
            p.Start();

            var readOut = Task.Run(async () =>
            {
                string? line;
                while ((line = await p.StandardOutput.ReadLineAsync()) != null)
                    onLine?.Invoke(line);
            });
            var readErr = Task.Run(async () =>
            {
                string? line;
                while ((line = await p.StandardError.ReadLineAsync()) != null)
                    onLine?.Invoke("[ERR] " + line);
            });

            await Task.WhenAll(readOut, readErr);
            await p.WaitForExitAsync();
            return p.ExitCode;
        }
    }
}