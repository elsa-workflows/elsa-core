using CShells.Features;
using Elsa.Expressions.CSharp.Activities;
using Elsa.Expressions.CSharp.Contracts;
using Elsa.Expressions.CSharp.Options;
using Elsa.Expressions.CSharp.Providers;
using Elsa.Expressions.CSharp.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.CSharp.ShellFeatures;

/// <summary>
/// Installs C# integration.
/// </summary>
[ShellFeature(
    DisplayName = "C# Expressions",
    Description = "Provides C# expression evaluation capabilities for workflows",
    DependsOn = ["Mediator", "Expressions", "MemoryCache"])]
[UsedImplicitly]
public class CSharpFeature : IShellFeature
{
    /// <summary>
    /// Configures the <see cref="CSharpOptions"/>.
    /// </summary>
    public Action<CSharpOptions> CSharpOptions { get; set; } = _ => { };

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure(CSharpOptions);

        // C# services.
        services
            .AddExpressionDescriptorProvider<CSharpExpressionDescriptorProvider>()
            .AddScoped<ICSharpEvaluator, CSharpEvaluator>();

        // Handlers.
        services.AddNotificationHandlersFrom<CSharpFeature>();

        // UI property handlers.
        services.AddScoped<IPropertyUIHandler, RunCSharpOptionsProvider>();
    }
}


