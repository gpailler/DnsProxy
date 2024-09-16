using System.Diagnostics;
using System.Net;
using ARSoft.Tools.Net.Dns;
using DnsProxy.Options;
using DnsProxy.Resolvers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace DnsProxy;

internal class DnsProxyService : BackgroundService
{
    private readonly IPAddress _listeningAddress;
    private readonly ILogger _logger;
    private readonly IRequestResolver _resolver;
    private readonly InterfacesMonitoring _monitoring;

    private CancellationToken _stoppingToken;

    public DnsProxyService(IOptions<ListenOptions> listenOptions, ILogger logger, IRequestResolver resolver, InterfacesMonitoring monitoring)
    {
        _listeningAddress = ((IPEndPoint)listenOptions.Value).Address;
        _logger = logger;
        _resolver = resolver;
        _monitoring = monitoring;

        _stoppingToken = CancellationToken.None;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        _monitoring.Start();
        try
        {
            using var server = new DnsServer(new UdpServerTransport(_listeningAddress), new TcpServerTransport(_listeningAddress));
            server.QueryReceived += OnQueryReceived;
            server.ExceptionThrown += OnExceptionThrown;
            server.Start();
            _logger.Information("Server started. Listening on {Options}", _listeningAddress);

            var tcs = new TaskCompletionSource<bool>();
            await using (stoppingToken.Register(() => tcs.SetResult(true)))
            {
                await tcs.Task;
            }

            server.Stop();
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
            _stoppingToken = CancellationToken.None;
        }
    }

    private Task OnExceptionThrown(object sender, ExceptionEventArgs e)
    {
        _logger.Error(e.Exception, "{Message}", e.Exception.Message);
        return Task.CompletedTask;
    }

    private async Task OnQueryReceived(object sender, QueryReceivedEventArgs e)
    {
        if (e.Query is not DnsMessage query)
        {
            _logger.Warning("Received invalid query");
            return;
        }

        var question = GetQuestionFromQuery(query);
        if (question == null)
        {
            return;
        }

        _logger.Debug("{Id} ==> {Question}", query.TransactionID, question);

        var stopWatch = Stopwatch.StartNew();
        var upstreamResponse = await _resolver.ResolveAsync(new Request(question), _stoppingToken);
        stopWatch.Stop();

        _logger.Debug("{Id} <=== Resolver {Resolver} ({Duration}ms)", query.TransactionID, upstreamResponse.ResolverName, stopWatch.ElapsedMilliseconds);

        e.Response = BuildResponse(query, upstreamResponse.Message);
    }

    private DnsMessageBase? BuildResponse(DnsMessage query, DnsMessage? responseMessage)
    {
        if (responseMessage == null)
        {
            _logger.Warning("{Id} <== null response", query.TransactionID);
            return null;
        }

        var response = query.CreateResponseInstance();
        foreach (DnsRecordBase record in responseMessage.AnswerRecords)
        {
            _logger.Debug("{Id} <=== {Record}", query.TransactionID, record);
            response.AnswerRecords.Add(record);
        }

        foreach (DnsRecordBase record in responseMessage.AdditionalRecords)
        {
            response.AdditionalRecords.Add(record);
        }

        foreach (DnsRecordBase record in responseMessage.AuthorityRecords)
        {
            response.AuthorityRecords.Add(record);
        }

        response.ReturnCode = responseMessage.ReturnCode;

        return response;
    }

    private DnsQuestion? GetQuestionFromQuery(DnsMessage query)
    {

        if (query.Questions.Count > 1)
        {
            _logger.Warning("Received more than 1 question");
            return null;
        }

        return query.Questions[0];
    }

    private record Request(DnsQuestion Question) : IRequest;
}
