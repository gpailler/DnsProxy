using System.Net;
using ARSoft.Tools.Net.Dns;
using DnsProxy.Options;
using Serilog;

namespace DnsProxy.Resolvers;

internal class RequestResolverFactory : IRequestResolverFactory
{
    private readonly ILogger _logger;

    public RequestResolverFactory(ILogger logger)
    {
        _logger = logger;
    }

    public IRequestResolver Create(EndPointOptions endpointOptions)
    {
        _logger.Debug("Creating resolver '{Resolver}'", endpointOptions);

        return new RequestResolver(endpointOptions);
    }

    private class RequestResolver : IRequestResolver
    {
        private readonly IPEndPoint _endpoint;
        private readonly DnsClient _client;

        public RequestResolver(EndPointOptions endpointOptions)
        {
            _endpoint = (IPEndPoint)endpointOptions;

            var transport = new IClientTransport[] { new UdpClientTransport(_endpoint.Port), new TcpClientTransport(_endpoint.Port) };
            _client = new DnsClient([_endpoint.Address], transport, true, endpointOptions.Timeout);
        }

        public async Task<IResponse> ResolveAsync(IRequest request, CancellationToken cancellationToken)
        {
            return new Response(
                await _client.ResolveAsync(request.Question.Name, request.Question.RecordType, request.Question.RecordClass, token: cancellationToken),
                _endpoint.ToString());
        }

        private record Response(DnsMessage? Message, string ResolverName) : IResponse;
    }
}
