using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.SasTokens.Contracts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.SasTokens.Features;

/// <summary>
/// Adds the SAS tokens feature to the workflow runtime.
/// </summary>
/// <inheritdoc />
public class SasTokensFeature(IModule module) : FeatureBase(module)
{
    /// <summary>
    /// Configures the <see cref="IDataProtectionBuilder"/> used for setting up data protection.
    /// Defaults to setting the application name to "Elsa Workflows".
    /// </summary>
    public Action<IDataProtectionBuilder> ConfigureDataProtectionBuilder { get; set; } = b => { b.SetApplicationName("Elsa Workflows"); };

    /// <summary>
    /// Factory method to create an instance of <see cref="ITokenService"/>.
    /// Defaults to creating a <see cref="DataProtectorTokenService"/> using dependency injection.
    /// </summary>
    public Func<IServiceProvider, ITokenService> TokenService { get; set; } = sp => ActivatorUtilities.CreateInstance<DataProtectorTokenService>(sp);

    /// <inheritdoc />
    public override void Apply()
    {
        var builder = Services.AddDataProtection();
        ConfigureDataProtectionBuilder(builder);

        Services.AddScoped(TokenService);
    }
}