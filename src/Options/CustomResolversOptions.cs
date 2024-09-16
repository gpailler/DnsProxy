using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace DnsProxy.Options;

internal partial class CustomResolversOptions : Collection<CustomResolversOptions.Item>
{
    public const string Key = "CustomResolvers";

    internal class Item : EndPointOptions
    {
        [Required]
        [StringLength(1000, MinimumLength = 1)]
        public string Rule { get; set; } = null!;
    }

    internal partial class Validator : ValidatorBase<CustomResolversOptions>
    {
        private readonly IValidateOptions<Item> _customResolverOptionsItemValidator = new ItemValidator();

        protected override IEnumerable<ValidateOptionsResult> ValidateOptions(string? name, CustomResolversOptions options)
        {
            return options.Select(item => _customResolverOptionsItemValidator.Validate(name, item));
        }

        [OptionsValidator]
        private partial class ItemValidator : IValidateOptions<Item>;
    }
}
