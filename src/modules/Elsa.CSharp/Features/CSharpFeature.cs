using Elsa.Common.Features;
using Elsa.CSharp.Contracts;
using Elsa.CSharp.Expressions;
using Elsa.CSharp.Providers;
using Elsa.CSharp.Services;
using Elsa.Expressions.Contracts;
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
    
    /// <inheritdoc />
    public override void Apply()
    {
        // C# services.
        Services
            .AddSingleton<IExpressionSyntaxProvider, CSharpExpressionSyntaxProvider>()
            .AddSingleton<ICSharpEvaluator, RoslynCSharpEvaluator>()
            .AddExpressionHandler<CSharpExpressionHandler, CSharpExpression>()
            ;

        // Handlers.
        Services.AddNotificationHandlersFrom<CSharpFeature>();
        
        // Activities.
        Module.AddActivitiesFrom<CSharpFeature>();
    }
}