using CShells.Features;
using Elsa.Expressions.CSharp.Contracts;
using Elsa.Expressions.CSharp.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.CSharp.ShellFeatures;

/// <summary>
/// Installs C# integration.
/// </summary>
[ShellFeature(
    DisplayName = "C# Expressions",
    Description = "Enables C# expression evaluation in workflows",
    DependsOn = ["Mediator", "Expressions", "MemoryCache"])]
public class CSharpFeature : IShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        // C# services.
        services
            .AddExpressionDescriptorProvider<CSharpExpressionDescriptorProvider>()
            .AddScoped<ICSharpEvaluator, CSharpEvaluator>()
            ;

        // Handlers.
        services.AddNotificationHandlersFrom<CSharpFeature>();

        // UI property handlers.
        services.AddScoped<IPropertyUIHandler, RunCSharpOptionsProvider>();
    }
}
