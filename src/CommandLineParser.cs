using System.CommandLine;
using Microsoft.Extensions.Hosting;
using Serilog.Core;
using Serilog.Events;

namespace DnsProxy;

internal class CommandLineParser
{
    private readonly IHost _host;
    private readonly WindowsServiceHelper _windowsServiceHelper;
    private readonly LoggingLevelSwitch _loggingLevelSwitch;

    public CommandLineParser(IHost host, WindowsServiceHelper windowsServiceHelper, LoggingLevelSwitch loggingLevelSwitch)
    {
        _host = host;
        _windowsServiceHelper = windowsServiceHelper;
        _loggingLevelSwitch = loggingLevelSwitch;
    }

    public Task<int> RunAsync(params string[] args)
    {
        return CreateRootCommand().InvokeAsync(args);
    }

    private RootCommand CreateRootCommand()
    {
        // Global options
        var debugOption = new Option<bool>("--debug", "Enable debug mode");

        // Service commands
        var serviceInstallCommand = new Command("install", "Install DNS Proxy service");
        serviceInstallCommand.SetHandler(debug => ConfigureLoggingAndRun(debug, () => _windowsServiceHelper.InstallAsync(debug ? debugOption.Aliases.First() : "")), debugOption);

        var serviceUninstallCommand = new Command("uninstall", "Uninstall DNS Proxy service");
        serviceUninstallCommand.SetHandler(debug => ConfigureLoggingAndRun(debug, () => _windowsServiceHelper.UninstallAsync()), debugOption);

        var serviceCommand = new Command("service", "Windows service commands");
        serviceCommand.AddCommand(serviceInstallCommand);
        serviceCommand.AddCommand(serviceUninstallCommand);

        // Root command
        var rootCommand = new RootCommand("DNS Proxy");
        rootCommand.AddGlobalOption(debugOption);
        rootCommand.Add(serviceCommand);
        rootCommand.SetHandler(debug => ConfigureLoggingAndRun(debug, () =>_host.RunAsync()), debugOption);

        return rootCommand;

        Task ConfigureLoggingAndRun(bool debug, Func<Task> action)
        {
            _loggingLevelSwitch.MinimumLevel = debug ? LogEventLevel.Debug : LogEventLevel.Information;

            return action();
        }
    }
}
