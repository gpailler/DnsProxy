using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DnsProxy.Options;
using DnsProxy.Resolvers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
            ILogger<CompositeRequestResolver> logger)
        {
            logger.LogInformation($"Creating default resolver '{defaultResolverOptions.Value}'");
            _defaultResolver = requestResolverFactory.Create(defaultResolverOptions.Value);
            _customResolvers = customResolversOptions.Value
                .Select(options =>
                {
                    logger.LogInformation($"Creating custom resolver '{options}' for '{options.Rule}'");
                    return customRequestResolverFactory.Create(options);
                }).ToArray();
        }

        public Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken)
        {
            var resolver = _customResolvers.FirstOrDefault(x => x.Match(request.Questions)) ??
                           _defaultResolver;

            return resolver.Resolve(request, cancellationToken);
        }
    }
}
