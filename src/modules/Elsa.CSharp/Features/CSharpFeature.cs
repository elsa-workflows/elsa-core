using Elsa.Common.Features;
using Elsa.CSharp.Contracts;
using Elsa.CSharp.Options;
using Elsa.CSharp.Providers;
using Elsa.CSharp.Services;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.CSharp.Features;

/// <summary>
/// Installs C# integration.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
public class CSharpFeature : FeatureBase
{
    /// <inheritdoc />
    public CSharpFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Configures the <see cref="CSharpOptions"/>.
    /// </summary>
    public Action<CSharpOptions> CSharpOptions { get; set; } = _ => { };

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(CSharpOptions);

        // C# services.
        Services
            .AddExpressionDescriptorProvider<CSharpExpressionDescriptorProvider>()
            .AddScoped<ICSharpEvaluator, CSharpEvaluator>()
            ;

        // Handlers.
        Services.AddNotificationHandlersFrom<CSharpFeature>();

        // Activities.
        Module.AddActivitiesFrom<CSharpFeature>();
    }
}