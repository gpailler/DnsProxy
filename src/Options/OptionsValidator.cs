using Microsoft.Extensions.Options;
using Serilog;

namespace DnsProxy.Options;

internal class OptionsValidator
{
    private readonly IStartupValidator _startupValidator;
    private readonly ILogger _logger;

    public OptionsValidator(IStartupValidator startupValidator, ILogger logger)
    {
        _startupValidator = startupValidator;
        _logger = logger;
    }

    public bool Validate()
    {
        try
        {
            _startupValidator.Validate();
        }
        catch (Exception ex)
        {
            var exceptions = ex is AggregateException aggregateException
                ? aggregateException.InnerExceptions.ToArray()
                : new[] { ex };

            _logger.Error("Settings validation failed:");
            foreach (var exception in exceptions)
            {
                _logger.Error("- {Error}", exception.Message);
            }

            return false;
        }

        return true;
    }
}
