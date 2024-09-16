using Microsoft.Extensions.Options;

namespace DnsProxy.Options;

internal partial class ListenOptions : EndPointOptions
{
    public const string Key = "Listen";

    [OptionsValidator]
    public partial class Validator : IValidateOptions<ListenOptions>;
}
