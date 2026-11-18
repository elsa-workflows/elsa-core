using CShells;
using CShells.Configuration;
using CShells.Features;
using CShells.Lifecycle;
using Elsa.Extensions;
using Elsa.Identity.Options;
using Elsa.Secrets.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Elsa.ModularServer.Web;

/// <summary>
/// Validates the identity signing and Secrets encryption options of every configured shell before the host starts listening.
/// </summary>
/// <remarks>
/// Elsa.Server.Web fails its own startup when the signing key is still a known default outside Development/Demo,
/// because <c>AddIdentityTokenOptionsValidation()</c> registers an <c>IStartupValidator</c> on the host container,
/// which the generic host runs while starting. In this host the same registration happens inside each shell's own
/// service collection (see Elsa.Identity ShellFeatures), and CShells builds shell providers lazily on the first
/// request (<c>ShellMiddleware</c> through <see cref="IShellRegistry.GetOrActivateAsync"/>). A shell provider is a
/// plain <c>BuildServiceProvider()</c>, whose <c>IStartupValidator</c> never runs, so validation only happened when
/// the options were first resolved inside a request: the placeholder key booted in Production and surfaced as a 500
/// on the first login (issue #9106).
/// <para>
/// This validation runs before the server accepts traffic, without activating any shell or running shell
/// initializers: it reads each shell blueprint, composes its <see cref="ShellSettings"/> (pure configuration, no
/// provider built) and resolves the effective features, including dependencies. For active Identity and Secrets
/// features it reads the same <see cref="ShellConfiguration"/> as the feature and runs the module's own validator,
/// with the same Development/Demo leniency. Invalid options throw <see cref="OptionsValidationException"/>.
/// </para>
/// </remarks>
internal static class IdentityTokenStartupValidation
{
    /// <summary>
    /// Validates enabled Identity and Secrets options for every shell the blueprint provider lists.
    /// </summary>
    /// <remarks>
    /// The first shell with invalid options throws, aborting the host before it listens - the same behavior
    /// Elsa.Server.Web has for the identical configuration.
    /// </remarks>
    public static async Task ValidateConfiguredShellsAsync(IServiceProvider rootServices, CancellationToken cancellationToken = default)
    {
        var registry = rootServices.GetRequiredService<IShellRegistry>();
        var rootConfiguration = rootServices.GetRequiredService<IConfiguration>();
        var assemblyProvider = rootServices.GetRequiredService<ModularFeatureAssemblyProvider>();
        var assemblies = await assemblyProvider.GetAssembliesAsync(rootServices, cancellationToken);
        var features = FeatureDiscovery.DiscoverFeatures(assemblies).ToDictionary(feature => feature.Id, StringComparer.OrdinalIgnoreCase);
        var dependencyResolver = new FeatureDependencyResolver();

        // Resolve the module's own validator the way a shell does: through DI, with the host environment injected,
        // so known-default keys keep working in Development/Demo.
        var validator = ActivatorUtilities.CreateInstance<ValidateIdentityTokenOptions>(rootServices);
        var secretsValidator = ActivatorUtilities.CreateInstance<ValidateSecretsOptions>(rootServices);

        string? cursor = null;
        do
        {
            var page = await registry.ListAsync(new ShellListQuery(cursor), cancellationToken);

            foreach (var summary in page.Items)
            {
                // Composing turns the blueprint into ShellSettings from configuration only: no shell provider is
                // built, no feature ConfigureServices and no shell initializer runs. A blueprint removed between
                // listing and lookup is skipped; ShellMiddleware would 404 it later.
                var blueprint = await registry.GetBlueprintAsync(summary.Name, cancellationToken);

                if (blueprint is null)
                    continue;

                var settings = await blueprint.Blueprint.ComposeAsync(cancellationToken);

                // Match shell activation: unknown optional features are skipped, and dependencies are effective features.
                var enabledFeatures = dependencyResolver.GetOrderedFeatures(settings.EnabledFeatures.Where(features.ContainsKey), features);

                if (enabledFeatures.Contains("Identity", StringComparer.OrdinalIgnoreCase))
                    ValidateOptions(validator, settings, rootConfiguration);

                if (enabledFeatures.Contains("Secrets", StringComparer.OrdinalIgnoreCase))
                {
                    var secretsOptions = new SecretsOptions();
                    new ShellConfiguration(settings, rootConfiguration).GetSection("Secrets").Bind(secretsOptions);
                    var validation = secretsValidator.Validate(Options.DefaultName, secretsOptions);
                    if (validation.Failed)
                        throw new OptionsValidationException(Options.DefaultName, typeof(SecretsOptions), validation.Failures);
                }
            }

            cursor = page.NextCursor;
        } while (cursor is not null);
    }

    private static void ValidateOptions(ValidateIdentityTokenOptions validator, ShellSettings settings, IConfiguration rootConfiguration)
    {
        var options = new IdentityTokenOptions();
        new ShellConfiguration(settings, rootConfiguration).GetSection("Identity").Bind(options);

        var result = validator.Validate(Options.DefaultName, options);

        if (result.Failed)
            throw new OptionsValidationException(Options.DefaultName, typeof(IdentityTokenOptions), result.Failures);
    }
}
