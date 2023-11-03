using Elsa.Common.Features;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Python.Contracts;
using Elsa.Python.Expressions;
using Elsa.Python.Options;
using Elsa.Python.Providers;
using Elsa.Python.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Python.Features;

/// <summary>
/// Installs Python integration.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
public class PythonFeature : FeatureBase
{
    /// <inheritdoc />
    public PythonFeature(IModule module) : base(module)
    {
    }
    
    /// <summary>
    /// Configures the <see cref="Options.PythonOptions"/>.
    /// </summary>
    public Action<PythonOptions> PythonOptions { get; set; } = _ => { };
    
    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(PythonOptions);
        
        // C# services.
        Services
            .AddSingleton<IExpressionSyntaxProvider, PythonExpressionSyntaxProvider>()
            .AddSingleton<IPythonEvaluator, IronPythonEvaluator>()
            .AddExpressionHandler<PythonExpressionHandler, PythonExpression>()
            ;

        // Handlers.
        Services.AddNotificationHandlersFrom<PythonFeature>();
        
        // Activities.
        Module.AddActivitiesFrom<PythonFeature>();
    }
}