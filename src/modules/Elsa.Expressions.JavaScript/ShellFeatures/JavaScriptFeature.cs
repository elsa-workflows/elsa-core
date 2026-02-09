using CShells.Features;
using Elsa.Expressions.JavaScript.Activities;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.JavaScript.Extensions;
using Elsa.Expressions.JavaScript.HostedServices;
using Elsa.Expressions.JavaScript.Options;
using Elsa.Expressions.JavaScript.Providers;
using Elsa.Expressions.JavaScript.Services;
using Elsa.Expressions.JavaScript.TypeDefinitions.Contracts;
using Elsa.Expressions.JavaScript.TypeDefinitions.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.JavaScript.ShellFeatures;

/// <summary>
/// Installs JavaScript integration.
/// </summary>
[ShellFeature(
    DisplayName = "JavaScript Expressions",
    Description = "Provides JavaScript expression evaluation capabilities for workflows",
    DependsOn = ["Mediator", "Expressions", "MemoryCache"])]
[UsedImplicitly]
public class JavaScriptFeature : IShellFeature
{
    /// <summary>
    /// Configures the Jint options.
    /// </summary>
    public Action<JintOptions> JintOptions { get; set; } = _ => { };

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure(JintOptions);

        // JavaScript services.
        services
            .AddScoped<IJavaScriptEvaluator, JintJavaScriptEvaluator>()
            .AddScoped<ITypeDefinitionService, TypeDefinitionService>()
            .AddExpressionDescriptorProvider<JavaScriptExpressionDescriptorProvider>();

        // Type definition services.
        services
            .AddScoped<ITypeDefinitionService, TypeDefinitionService>()
            .AddScoped<ITypeDescriber, TypeDescriber>()
            .AddScoped<ITypeDefinitionDocumentRenderer, TypeDefinitionDocumentRenderer>()
            .AddSingleton<ITypeAliasRegistry, TypeAliasRegistry>()
            .AddFunctionDefinitionProvider<CommonFunctionsDefinitionProvider>()
            .AddFunctionDefinitionProvider<ActivityOutputFunctionsDefinitionProvider>()
            .AddFunctionDefinitionProvider<RunJavaScriptFunctionsDefinitionProvider>()
            .AddTypeDefinitionProvider<CommonTypeDefinitionProvider>()
            .AddTypeDefinitionProvider<VariableTypeDefinitionProvider>()
            .AddTypeDefinitionProvider<WorkflowVariablesTypeDefinitionProvider>()
            .AddVariableDefinitionProvider<WorkflowVariablesVariableProvider>();

        // Handlers.
        services.AddNotificationHandlersFrom<JavaScriptFeature>();

        // Type Script definitions.
        services.AddFunctionDefinitionProvider<InputFunctionsDefinitionProvider>();

        // UI property handlers.
        services.AddScoped<IPropertyUIHandler, RunJavaScriptOptionsProvider>();

        // Hosted services.
        services.AddHostedService<RegisterVariableTypesWithJavaScriptHostedService>();
    }
}


