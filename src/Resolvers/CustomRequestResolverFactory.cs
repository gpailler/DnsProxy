using System.Text.RegularExpressions;
using DnsProxy.Options;

namespace DnsProxy.Resolvers;

internal class CustomRequestResolverFactory : ICustomRequestResolverFactory
{
    private readonly IRequestResolverFactory _requestResolverFactory;

    public CustomRequestResolverFactory(IRequestResolverFactory requestResolverFactory)
    {
        _requestResolverFactory = requestResolverFactory;
    }

    public ICustomRequestResolver Create(CustomResolversOptions.Item customResolverOptions)
    {
        return new CustomRequestResolver(customResolverOptions.Rule, _requestResolverFactory.Create(customResolverOptions));
    }

    private class CustomRequestResolver : ICustomRequestResolver
    {
        private readonly IRequestResolver _resolver;
        private readonly Regex _rule;

        public CustomRequestResolver(string? rule, IRequestResolver resolver)
        {
            if (rule == null)
            {
                throw new ArgumentException($"Empty rule");
            }

            _rule = new Regex(rule);
            _resolver = resolver;
        }

        public bool Match(IRequest request) => _rule.IsMatch(request.Question.Name.ToString());

        public Task<IResponse> ResolveAsync(IRequest request, CancellationToken cancellationToken) => _resolver.ResolveAsync(request, cancellationToken);
    }
}
