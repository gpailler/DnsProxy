using DnsProxy;
using DnsProxy.Options;
using DnsProxy.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;

var builder = Host.CreateApplicationBuilder(args);

// Logging
 builder.Services.AddSerilog((provider, configuration) =>
{
    var env = provider.GetRequiredService<IHostEnvironment>();
    var logFile = Path.Combine(env.ContentRootPath, Path.ChangeExtension(env.ApplicationName,"log"));

    configuration
        .MinimumLevel.ControlledBy(provider.GetRequiredService<LoggingLevelSwitch>())
        .Enrich.WithProcessId()
        .WriteTo.File(
            logFile,
            shared: true,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [PID: {ProcessId}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}");
});

// Options
builder.Services.Configure<ListenOptions>(builder.Configuration.GetRequiredSection(ListenOptions.Key));
builder.Services.Configure<DefaultResolverOptions>(builder.Configuration.GetRequiredSection(DefaultResolverOptions.Key));
builder.Services.Configure<CustomResolversOptions>(builder.Configuration.GetRequiredSection(CustomResolversOptions.Key));
builder.Services.Configure<MonitoringOptions>(builder.Configuration.GetRequiredSection(MonitoringOptions.Key));

// Services
builder.Services.AddTransient<IRequestResolver, CompositeRequestResolver>();
builder.Services.AddScoped<IRequestResolverFactory, RequestResolverFactory>();
builder.Services.AddScoped<ICustomRequestResolverFactory, CustomRequestResolverFactory>();
builder.Services.AddSingleton<WindowsServiceHelper>();
builder.Services.AddSingleton<CommandLineParser>();
builder.Services.AddSingleton<InterfacesMonitoring>();
builder.Services.AddSingleton<LoggingLevelSwitch>();
builder.Services.AddHostedService<DnsProxyService>();

// Service configuration
builder.Services.AddWindowsService(options => options.ServiceName = WindowsServiceHelper.ServiceName);

// Run app
using var host = builder.Build();

return await host.Services.GetRequiredService<CommandLineParser>().RunAsync(args);
