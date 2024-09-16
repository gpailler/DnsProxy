using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace DnsProxy.Options;

internal partial class MonitoringOptions
{
    public const string Key = "Monitoring";

    [Required]
    [Range(100, 3600_000)]
    public int Interval { get; set; } = 5000;

    public string[]? Interfaces { get; set; }

    internal partial class Validator : ValidatorBase<MonitoringOptions>
    {
        private readonly IValidateOptions<MonitoringOptions> _defaultValidator = new DefaultValidator();

        protected override IEnumerable<ValidateOptionsResult> ValidateOptions(string? name, MonitoringOptions options)
        {
            List<ValidateOptionsResult> results = new();
            results.Add(_defaultValidator.Validate(name, options));

            if (options.Interfaces != null && options.Interfaces.Any(x => x.Length == 0))
            {
                results.Add(ValidateOptionsResult.Fail($"{nameof(Interfaces)}: The field {nameof(MonitoringOptions)}.{nameof(Interfaces)} must not contain empty values."));
            }

            return results;
        }

        [OptionsValidator]
        private partial class DefaultValidator : IValidateOptions<MonitoringOptions>;
    }
}
