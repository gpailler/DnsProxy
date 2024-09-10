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

    public Task InstallAsync(string extraArguments = "")
    {
        if (!Helpers.IsElevatedAccount())
        {
            _logger.Error("This command must be executed from an elevated command prompt.");
            return Task.CompletedTask;
        }

        _logger.Information($"Installing and starting {ServiceName}...");
        string appFilePath = Path.Combine(_env.ContentRootPath, Path.ChangeExtension(_env.ApplicationName, "exe"));
        extraArguments = string.IsNullOrEmpty(extraArguments) ? "" : $" {extraArguments}";
        if (RunSc($@"create ""{ServiceName}"" binpath= ""{appFilePath}{extraArguments}"" start= delayed-auto"))
        {
            _logger.Information($"Starting {ServiceName}...");
            RunSc($@"start ""{ServiceName}""");
        }

        return Task.CompletedTask;
    }

    public Task UninstallAsync()
    {
        if (!Helpers.IsElevatedAccount())
        {
            _logger.Error("This command must be executed from an elevated command prompt.");
            return Task.CompletedTask;
        }

        _logger.Information($"Stopping {ServiceName}...");
        RunSc($@"stop ""{ServiceName}""");

        _logger.Information($"Uninstalling {ServiceName}...");
        RunSc($@"delete ""{ServiceName}""");
        return Task.CompletedTask;
    }

    private bool RunSc(string arguments)
    {
        return Helpers.Run("sc.exe", arguments, _logger);
    }
}
