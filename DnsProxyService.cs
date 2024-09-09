using System.Net.Sockets;
using DNS.Client;
using DNS.Client.RequestResolver;
using DNS.Server;
using DnsProxy.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace DnsProxy;

internal class DnsProxyService : BackgroundService
{
    private readonly ListenOptions _options;
    private readonly ILogger _logger;
    private readonly IRequestResolver _resolver;
    private readonly InterfacesMonitoring _monitoring;

    public DnsProxyService(IOptions<ListenOptions> options, ILogger logger, IRequestResolver resolver, InterfacesMonitoring monitoring)
    {
        _options = options.Value;
        _logger = logger;
        _resolver = resolver;
        _monitoring = monitoring;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _monitoring.Start();
        try
        {
            await Listen(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // When the stopping token is canceled, for example, a call made from services.msc,
            // we shouldn't exit with a non-zero exit code. In other words, this is expected...
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "{Message}", ex.Message);

            // Terminates this process and returns an exit code to the operating system.
            // This is required to avoid the 'BackgroundServiceExceptionBehavior', which
            // performs one of two scenarios:
            // 1. When set to "Ignore": will do nothing at all, errors cause zombie services.
            // 2. When set to "StopHost": will cleanly stop the host, and log errors.
            //
            // In order for the Windows Service Management system to leverage configured
            // recovery options, we need to terminate the process with a non-zero exit code.
            Environment.Exit(1);
        }
        finally
        {
            _monitoring.Stop();
        }
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

            if (e.Exception is SocketException { ErrorCode: 10051 or 10065 or 995 })
            {
                // System.Net.Sockets.SocketException (10051): A socket operation was attempted to an unreachable network.
                // System.Net.Sockets.SocketException (10065): A socket operation was attempted to an unreachable host.
                // System.Net.Sockets.SocketException (995): The I/O operation has been aborted because of either a thread exit or an application request.
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
        server.Requested += (_, e) => _logger.Debug($"Request: {e.Request}");
        server.Responded += (_, e) => _logger.Debug($"Response: {e.Request} => {e.Response}");

        _logger.Information($"Starting server on '{_options}'");
        await server.Listen(_options);
    }
}
