using Microsoft.Extensions.Hosting;
using NJsonSchema;
using NJsonSchema.References;
using NJsonSchema.Validation;
using Serilog;

namespace DnsProxy;

internal class SettingsValidator
{
    private readonly IHostEnvironment _environment;
    private readonly ILogger _logger;

    public SettingsValidator(IHostEnvironment environment, ILogger logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public async Task<bool> ValidateSettings()
    {
        string settingsFile = Path.Combine(_environment.ContentRootPath, "appsettings.json");
        _logger.Debug("Validating settings file {SettingsFile}.", settingsFile);

        try
        {
            if (!File.Exists(settingsFile))
            {
                _logger.Error("Settings file is missing.");
                return false;
            }

            string jsonSchemaFile = Path.Combine(_environment.ContentRootPath, "appsettings.schema.json");
            if (!File.Exists(jsonSchemaFile))
            {
                _logger.Warning("Settings cannot be validated because schema file is missing ({JsonSchemaFile}.",
                    jsonSchemaFile);
            }
            else
            {
                var jsonSchema = await JsonSchema.FromFileAsync(jsonSchemaFile, x => new NoUrlReferenceResolver(x));
                var validationErrors = jsonSchema.Validate(await File.ReadAllTextAsync(settingsFile));
                if (validationErrors.Count > 0)
                {
                    _logger.Error("Settings file is invalid.");
                    foreach (var validationError in validationErrors)
                    {
                        if (validationError is ChildSchemaValidationError childValidationError)
                        {
                            foreach (var error in childValidationError.Errors.SelectMany(x => x.Value))
                            {
                                _logger.Error(error.ToString());
                            }
                        }
                        else
                        {
                            _logger.Error(validationError.ToString());
                        }
                    }

                    return false;
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error validating settings.");

            return false;
        }
    }

    private class NoUrlReferenceResolver : JsonReferenceResolver
    {
        public NoUrlReferenceResolver(JsonSchema schemaAppender)
            : base(new JsonSchemaAppender(schemaAppender, new DefaultTypeNameGenerator()))
        {
        }

        public override Task<IJsonReference> ResolveUrlReferenceAsync(string url, CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.FromResult<IJsonReference>(new JsonSchema());
        }
    }
}
