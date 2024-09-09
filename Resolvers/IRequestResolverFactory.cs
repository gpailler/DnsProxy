using DNS.Client.RequestResolver;
using DnsProxy.Options;

namespace DnsProxy.Resolvers;

internal interface IRequestResolverFactory
{
    IRequestResolver Create(EndPointOptions endpointOptions);
}
