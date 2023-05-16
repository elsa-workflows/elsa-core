using Elsa.Environments.Contracts;
using Elsa.Environments.Options;
using Elsa.Environments.Providers;
using Elsa.Environments.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Environments.Features;

/// <summary>
/// Installs the <see cref="EnvironmentsFeature"/> feature.
/// </summary>
[PublicAPI]
public class EnvironmentsFeature : FeatureBase
{
    /// <inheritdoc />
    public EnvironmentsFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A delegate to configure the <see cref="EnvironmentsOptions"/>.
    /// </summary>
    public Action<EnvironmentsOptions> EnvironmentsOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly(GetType());
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(EnvironmentsOptions);

        Services.AddSingleton<IEnvironmentsProvider, ConfigurationEnvironmentsProvider>();
        Services.AddSingleton<IEnvironmentsManager, DefaultEnvironmentsManager>();
    }
}