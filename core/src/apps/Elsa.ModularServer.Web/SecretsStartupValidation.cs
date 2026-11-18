using CShells;
using CShells.Configuration;
using CShells.Lifecycle;
using Elsa.Secrets.Options;
using Microsoft.Extensions.Options;

namespace Elsa.ModularServer.Web;

internal static class SecretsStartupValidation
{
    public static async Task ValidateConfiguredShellsAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var registry = services.GetRequiredService<IShellRegistry>();
        var rootConfiguration = services.GetRequiredService<IConfiguration>();
        var validator = ActivatorUtilities.CreateInstance<ValidateSecretsOptions>(services);
        string? cursor = null;

        do
        {
            var page = await registry.ListAsync(new ShellListQuery(cursor), cancellationToken);
            foreach (var summary in page.Items)
            {
                // Read each blueprint without activating its lazily built shell provider.
                var blueprint = await registry.GetBlueprintAsync(summary.Name, cancellationToken);
                if (blueprint is null)
                    continue;

                var settings = await blueprint.Blueprint.ComposeAsync(cancellationToken);
                if (!settings.EnabledFeatures.Contains("Secrets", StringComparer.OrdinalIgnoreCase) ||
                    settings.DisabledFeatures.Contains("Secrets", StringComparer.OrdinalIgnoreCase))
                    continue;

                var options = new SecretsOptions();
                new ShellConfiguration(settings, rootConfiguration).GetSection("Secrets").Bind(options);
                var result = validator.Validate(Options.DefaultName, options);
                if (result.Failed)
                    throw new OptionsValidationException(Options.DefaultName, typeof(SecretsOptions), result.Failures);
            }

            cursor = page.NextCursor;
        } while (cursor is not null);
    }
}
