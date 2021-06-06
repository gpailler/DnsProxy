using DNS.Client.RequestResolver;
using DnsProxy.Options;
using Microsoft.Extensions.Logging;

namespace DnsProxy.Resolvers
{
    internal class RequestResolverFactory : IRequestResolverFactory
    {
        private readonly ILogger<RequestResolverFactory> _logger;

        public RequestResolverFactory(ILogger<RequestResolverFactory> logger)
        {
            _logger = logger;
        }

        public IRequestResolver Create(EndPointOptions endpointOptions)
        {
            _logger.LogDebug($"Creating UDP resolver '{endpointOptions}'");

            return new UdpRequestResolver(endpointOptions);
        }
    }
}
