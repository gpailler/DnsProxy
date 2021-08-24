using System;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Server;
using DnsProxy.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Topshelf;

namespace DnsProxy
{
    internal class DnsProxyService
    {
        private readonly ListenOptions _options;
        private readonly ILogger<DnsProxyService> _logger;
        private readonly IRequestResolver _resolver;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _listenTask;

        public DnsProxyService(IOptions<ListenOptions> options, ILogger<DnsProxyService> logger, IRequestResolver resolver)
        {
            _options = options.Value;
            _logger = logger;
            _resolver = resolver;

            string? semVer = typeof(DnsProxyService).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            string? informationalVersion = typeof(DnsProxyService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            _logger.LogInformation($"DnsProxy v{semVer} ({informationalVersion})");
        }

        public bool Start(HostControl hostControl)
        {
            _logger.LogInformation("Starting...");

            _cancellationTokenSource = new CancellationTokenSource();
            _listenTask = Task
                .Run(() => Listen(_cancellationTokenSource.Token))
                .ContinueWith(x =>
                {
                    if (x.IsFaulted)
                    {
                        _logger.LogCritical(x.Exception, string.Empty);
                        hostControl.Stop(TopshelfExitCode.AbnormalExit);
                    }
                });

            return true;
        }

        public bool Stop(HostControl hostControl)
        {
            _logger.LogInformation("Stopping...");

            _cancellationTokenSource?.Cancel();
            _listenTask?.Wait();

            return true;
        }

        private async Task Listen(CancellationToken cancellationToken)
        {
            var server = new DnsServer(_resolver);
            cancellationToken.Register(() => server.Dispose());
            server.Errored += (_, e) =>
            {
                if (e.Exception is ArgumentException)
                {
                    return;
                }

                if (e.Exception is SocketException { ErrorCode: 10051 or 10065 })
                {
                    // System.Net.Sockets.SocketException (10051): A socket operation was attempted to an unreachable network.
                    // System.Net.Sockets.SocketException (10065): A socket operation was attempted to an unreachable host.
                    return;
                }

                if (e.Exception is ResponseException)
                {
                    return;
                }

                if (e.Exception is OperationCanceledException)
                {
                    return;
                }

                throw e.Exception;
            };
            server.Requested += (_, e) => _logger.LogDebug($"Request: {e.Request}");
            server.Responded += (_, e) => _logger.LogDebug($"Response: {e.Request} => {e.Response}");

            _logger.LogInformation($"Starting server on '{_options}'");
            await server.Listen(_options);
        }
    }
}
