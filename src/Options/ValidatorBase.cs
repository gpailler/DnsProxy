using Microsoft.Extensions.Options;

namespace DnsProxy.Options;

internal abstract class ValidatorBase<TOptions> : IValidateOptions<TOptions> where TOptions : class
{
    public ValidateOptionsResult Validate(string? name, TOptions options)
    {
        var results = ValidateOptions(name, options)
            .Where(x => x.Failed)
            .ToArray();
        if (results.Length > 0)
        {
            return ValidateOptionsResult.Fail($"{typeof(TOptions).Name} > {string.Join("; ", results.Select(x => x.FailureMessage))}");
        }

        return ValidateOptionsResult.Success;
    }

    protected abstract IEnumerable<ValidateOptionsResult> ValidateOptions(string? name, TOptions options);
}
