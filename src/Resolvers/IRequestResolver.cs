using ARSoft.Tools.Net.Dns;

namespace DnsProxy.Resolvers;

internal interface IRequestResolver
{
    Task<IResponse> ResolveAsync(IRequest request, CancellationToken cancellationToken);
}

internal interface IRequest
{
    DnsQuestion Question { get; }
}

internal interface IResponse
{
    DnsMessage? Message { get; }

    string ResolverName { get; }
}
