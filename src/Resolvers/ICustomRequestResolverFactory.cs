using DnsProxy.Options;

namespace DnsProxy.Resolvers;

internal interface ICustomRequestResolverFactory
{
    ICustomRequestResolver Create(CustomResolversOptions.Item customResolverOptions);
}
