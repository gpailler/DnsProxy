using DnsProxy.Options;
using DnsProxy.Resolvers;
using Microsoft.Extensions.Options;
using Serilog;

namespace DnsProxy
{
    internal class CompositeRequestResolver : IRequestResolver
    {
        private readonly Lazy<IRequestResolver> _defaultResolver;
        private readonly Lazy<ICustomRequestResolver[]> _customResolvers;

        public CompositeRequestResolver(
            IOptions<DefaultResolverOptions> defaultResolverOptions,
            IOptions<CustomResolversOptions> customResolversOptions,
            IRequestResolverFactory requestResolverFactory,
            ICustomRequestResolverFactory customRequestResolverFactory,
            ILogger logger)
        {
            _defaultResolver = new Lazy<IRequestResolver>(() =>
            {
                logger.Information("Creating default resolver...");
                return requestResolverFactory.Create(defaultResolverOptions.Value);
            });

            _customResolvers = new Lazy<ICustomRequestResolver[]>(() =>
            {
                logger.Information("Creating custom resolvers...");
                return customResolversOptions.Value
                    .Select(options =>
                    {
                        logger.Information("Creating custom resolver for '{Rule}'", options.Rule);
                        return customRequestResolverFactory.Create(options);
                    }).ToArray();
            });
        }

        public async Task<IResponse> ResolveAsync(IRequest request, CancellationToken cancellationToken)
        {
            var resolver = _customResolvers.Value.FirstOrDefault(x => x.Match(request)) ??
                           _defaultResolver.Value;

            return await resolver.ResolveAsync(request, cancellationToken);
        }
    }
}
