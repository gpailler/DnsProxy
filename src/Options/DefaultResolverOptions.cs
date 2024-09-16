using Microsoft.Extensions.Options;

namespace DnsProxy.Options;

internal partial class DefaultResolverOptions : EndPointOptions
{
    public const string Key = "DefaultResolver";

    [OptionsValidator]
    internal partial class Validator : IValidateOptions<DefaultResolverOptions>;
}
