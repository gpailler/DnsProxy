using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace DnsProxy;

internal class WindowsServiceHelper
{
    public const string ServiceName = "DNS Proxy service";

    private readonly IHostEnvironment _env;
    private readonly ILogger _logger;

    public WindowsServiceHelper(IHostEnvironment env, ILogger logger)
    {
        _env = env;
        _logger = logger;
    }

    public async Task InstallAsync(string extraArguments = "")
    {
        if (!Helpers.IsElevatedAccount())
        {
            _logger.Error("This command must be executed from an elevated command prompt.");
            return;
        }

        _logger.Information($"Installing and starting {ServiceName}...");
        string appFilePath = Path.Combine(_env.ContentRootPath, Path.ChangeExtension(_env.ApplicationName, "exe"));
        extraArguments = string.IsNullOrEmpty(extraArguments) ? "" : $" {extraArguments}";
        if (await RunSc($@"create ""{ServiceName}"" binpath= ""{appFilePath}{extraArguments}"" start= delayed-auto"))
        {
            _logger.Information($"Starting {ServiceName}...");
            await RunSc($@"start ""{ServiceName}""");
        }
    }

    public async Task UninstallAsync()
    {
        if (!Helpers.IsElevatedAccount())
        {
            _logger.Error("This command must be executed from an elevated command prompt.");
            return;
        }

        _logger.Information($"Stopping {ServiceName}...");
        await RunSc($@"stop ""{ServiceName}""");

        _logger.Information($"Uninstalling {ServiceName}...");
        await RunSc($@"delete ""{ServiceName}""");
    }

    private async Task<bool> RunSc(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = startInfo;

        process.OutputDataReceived += (_, e) => Log(e.Data, _logger.Information);
        process.ErrorDataReceived += (_, e) => Log(e.Data, _logger.Warning);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync();

        _logger.Information("Command execution result: {ExitCode}", process.ExitCode);

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
