using System.Collections.Generic;
using DNS.Client.RequestResolver;
using DNS.Protocol;

namespace DnsProxy.Resolvers
{
    public interface ICustomRequestResolver : IRequestResolver
    {
        bool Match(IEnumerable<Question> questions);
    }
}
