using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DNS.Client.RequestResolver;
using DNS.Protocol;
using DnsProxy.Options;

namespace DnsProxy.Resolvers
{
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
                _resolver = resolver;

                if (rule == null)
                {
                    throw new ArgumentException($"Empty rule");
                }

                _rule = new Regex(rule);
            }

            public bool Match(IEnumerable<Question> questions)
            {
                return questions.All(question => _rule.IsMatch(question.Name.ToString()));
            }

            public Task<IResponse> Resolve(IRequest request, CancellationToken cancellationToken = new())
            {
                return _resolver.Resolve(request, cancellationToken);
            }
        }
    }
}
