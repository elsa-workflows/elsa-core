using CShells.Features;
using Elsa.Expressions.Python.Contracts;
using Elsa.Expressions.Python.Providers;
using Elsa.Expressions.Python.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.Python.ShellFeatures;

/// <summary>
/// Installs Python integration.
/// </summary>
[ShellFeature(
    DisplayName = "Python Expressions",
    Description = "Enables Python expression evaluation in workflows using Python.NET",
    DependsOn = ["Mediator", "Expressions"])]
public class PythonFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Python services.
        services
            .AddScoped<IPythonEvaluator, PythonNetPythonEvaluator>()
            .AddExpressionDescriptorProvider<PythonExpressionDescriptorProvider>()
            ;

        // Handlers.
        services.AddNotificationHandlersFrom<PythonFeature>();

        // UI property handlers.
        services.AddScoped<IPropertyUIHandler, RunPythonOptionsProvider>();
    }
}
