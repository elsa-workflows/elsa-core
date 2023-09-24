using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using Elsa.SasTokens.Extensions;
using Microsoft.AspNetCore.DataProtection;

namespace Elsa.SasTokens.Features;

/// <summary>
/// Adds the SAS tokens feature to the workflow runtime.
/// </summary>
public class SasTokensFeature : FeatureBase
{
    /// <inheritdoc />
    public SasTokensFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Gets or sets the data protection provider used to create the <see cref="Microsoft.AspNetCore.DataProtection.IDataProtector"/> used to encrypt and decrypt the SAS tokens.
    /// </summary>
    public Func<IServiceProvider, IDataProtectionProvider> DataProtectionProvider { get; set; } = _ => Microsoft.AspNetCore.DataProtection.DataProtectionProvider.Create("Elsa Workflows");

    /// <inheritdoc />
    public override void Apply()
    {
        Services.AddSasTokens(DataProtectionProvider);
    }
}