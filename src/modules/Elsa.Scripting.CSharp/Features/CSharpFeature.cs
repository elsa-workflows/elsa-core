using Elsa.Caching.Features;
using Elsa.Common.Features;
using Elsa.Scripting.CSharp.Activities;
using Elsa.Scripting.CSharp.Contracts;
using Elsa.Scripting.CSharp.Options;
using Elsa.Scripting.CSharp.Providers;
using Elsa.Scripting.CSharp.Services;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Scripting.CSharp.Features;

/// <summary>
/// Installs C# integration.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
[DependsOn(typeof(MemoryCacheFeature))]
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
        
        // UI property handlers.
        Services.AddScoped<IPropertyUIHandler, RunCSharpOptionsProvider>();
    }
}