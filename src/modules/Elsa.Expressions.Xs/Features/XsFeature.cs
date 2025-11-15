using Elsa.Caching.Features;
using Elsa.Common.Features;
using Elsa.Expressions.Features;
using Elsa.Expressions.Xs.Contracts;
using Elsa.Expressions.Xs.Options;
using Elsa.Expressions.Xs.Providers;
using Elsa.Expressions.Xs.Services;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Xs.Features;

/// <summary>
/// Installs XS integration.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
[DependsOn(typeof(MemoryCacheFeature))]
public class XsFeature : FeatureBase
{
    /// <inheritdoc />
    public XsFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Configures the <see cref="XsOptions"/>.
    /// </summary>
    public Action<XsOptions> XsOptionsConfig { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(XsOptionsConfig);

        // XS services.
        Services
            .AddExpressionDescriptorProvider<XsExpressionDescriptorProvider>()
            .AddScoped<IXsEvaluator, XsEvaluator>()
            ;
    }
}
