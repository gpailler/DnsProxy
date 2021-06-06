using System;
using DNS.Client.RequestResolver;
using DnsProxy.Options;
using DnsProxy.Resolvers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Extensions.Logging;
using Topshelf;
using Topshelf.MicrosoftDependencyInjection;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace DnsProxy
{
    class Program
    {
        static void Main(string[] args)
        {
            var provider = RegisterServices();

            var host = HostFactory.New(hostConfig =>
            {
                hostConfig.UseNLog(LogManager.LogFactory);
                hostConfig.UseServiceProvider(provider);
                hostConfig.Service<DnsProxyService>(config =>
                {
                    config.ConstructUsingServiceProvider();
                    config.WhenStarted((instance, hostControl) => instance.Start(hostControl));
                    config.WhenStopped((instance, hostControl) => instance.Stop(hostControl));
                });
                hostConfig.RunAsNetworkService();
                hostConfig.SetDescription("Dns proxy service");
            });

            var returnCode = host.Run();
            Environment.Exit((int)returnCode);
        }

        private static IServiceProvider RegisterServices()
        {
            var services = new ServiceCollection();

            var config = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.base.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddNLog(config);
                loggingBuilder.SetMinimumLevel(LogLevel.Trace);

                // Read NLog configuration from appsettings.json
                LogManager.Configuration = new NLogLoggingConfiguration(config.GetSection("NLog"));
            });

            services.Configure<ListenOptions>(config.GetSection(ListenOptions.Key));
            services.Configure<DefaultResolverOptions>(config.GetSection(DefaultResolverOptions.Key));
            services.Configure<CustomResolversOptions>(config.GetSection(CustomResolversOptions.Key));

            services.AddSingleton<DnsProxyService>();
            services.AddTransient<IRequestResolver, CompositeRequestResolver>();
            services.AddScoped<IRequestResolverFactory, RequestResolverFactory>();
            services.AddScoped<ICustomRequestResolverFactory, CustomRequestResolverFactory>();

            return services.BuildServiceProvider();
        }
    }
}
