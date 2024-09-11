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
        }

        _logger.Information($"Installing and starting {ServiceName}...");
        string appFilePath = Path.Combine(_env.ContentRootPath, Path.ChangeExtension(_env.ApplicationName, "exe"));
        extraArguments = string.IsNullOrEmpty(extraArguments) ? "" : $" {extraArguments}";
        if (await RunScAsync($@"create ""{ServiceName}"" binpath= ""{appFilePath}{extraArguments}"" start= delayed-auto"))
        {
            _logger.Information($"Starting {ServiceName}...");
            await RunScAsync($@"start ""{ServiceName}""");
        }
    }

    public async Task UninstallAsync()
    {
        if (!Helpers.IsElevatedAccount())
        {
            _logger.Error("This command must be executed from an elevated command prompt.");
        }

        _logger.Information($"Stopping {ServiceName}...");
        await RunScAsync($@"stop ""{ServiceName}""");

        _logger.Information($"Uninstalling {ServiceName}...");
        await RunScAsync($@"delete ""{ServiceName}""");
    }

    private Task<bool> RunScAsync(string arguments)
    {
        return Helpers.RunAsync("sc.exe", arguments, _logger);
    }
}
