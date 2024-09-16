using DnsProxy.Options;
using DnsProxy.Resolvers;
using Microsoft.Extensions.Options;
using Serilog;

namespace DnsProxy
{
    internal class CompositeRequestResolver : IRequestResolver
    {
        private readonly IRequestResolver _defaultResolver;
        private readonly ICustomRequestResolver[] _customResolvers;

        public CompositeRequestResolver(
            IOptions<DefaultResolverOptions> defaultResolverOptions,
            IOptions<CustomResolversOptions> customResolversOptions,
            IRequestResolverFactory requestResolverFactory,
            ICustomRequestResolverFactory customRequestResolverFactory,
            ILogger logger)
        {
            logger.Information("Creating default resolver...");
            _defaultResolver = requestResolverFactory.Create(defaultResolverOptions.Value);
            _customResolvers = customResolversOptions.Value
                .Select(options =>
                {
                    logger.Information("Creating custom resolver for '{Rule}'",options.Rule);
                    return customRequestResolverFactory.Create(options);
                }).ToArray();
        }

        public async Task<IResponse> ResolveAsync(IRequest request, CancellationToken cancellationToken)
        {
            var resolver = _customResolvers.FirstOrDefault(x => x.Match(request)) ??
                           _defaultResolver;

            return await resolver.ResolveAsync(request, cancellationToken);
        }
    }
}
