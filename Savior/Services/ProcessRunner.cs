using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Savior.Services
{
    public static class ProcessRunner
    {
        public static async Task<int> RunHiddenAsync(string file, string args, Action<string>? onLine = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = file,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
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