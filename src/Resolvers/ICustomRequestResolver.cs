namespace DnsProxy.Resolvers;

internal interface ICustomRequestResolver : IRequestResolver
{
    bool Match(IRequest request);
}
