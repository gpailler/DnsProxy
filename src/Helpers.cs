using System.Diagnostics;
using System.Security.Principal;
using Serilog;

namespace DnsProxy;

public static class Helpers
{
    public static bool IsElevatedAccount()
    {
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static async Task<bool> RunAsync(string fileName, string arguments, ILogger logger)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = startInfo;

        process.OutputDataReceived += (_, e) => Log(e.Data, logger.Information);
        process.ErrorDataReceived += (_, e) => Log(e.Data, logger.Warning);

        logger.Information("Running command: {Filename} {Arguments}", fileName, arguments);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync();

        logger.Information("Command execution result: {ExitCode}", process.ExitCode);

        return process.ExitCode == 0;

        void Log(string? data, Action<string> logger)
        {
            if (data != null)
            {
                logger.Invoke(data);
            }
        }
    }
}
