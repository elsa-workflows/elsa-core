using CShells.FastEndpoints.Features;
using CShells.Features;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.JavaScript.Extensions;
using Elsa.Expressions.JavaScript.Providers;
using Elsa.Expressions.JavaScript.Services;
using Elsa.Expressions.JavaScript.TypeDefinitions.Contracts;
using Elsa.Expressions.JavaScript.TypeDefinitions.Services;
using Elsa.Extensions;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.JavaScript.ShellFeatures;

/// <summary>
/// Installs JavaScript integration.
/// </summary>
[ShellFeature(
    DisplayName = "JavaScript Expressions",
    Description = "Enables JavaScript expression evaluation in workflows using Jint",
    DependsOn = ["Mediator", "Expressions", "MemoryCache"])]
public class JavaScriptFeature : IFastEndpointsShellFeature
{
    public void ConfigureServices(IServiceCollection services)
    {
        // JavaScript services.
        services
            .AddScoped<IJavaScriptEvaluator, JintJavaScriptEvaluator>()
            .AddScoped<ITypeDefinitionService, TypeDefinitionService>()
            .AddExpressionDescriptorProvider<JavaScriptExpressionDescriptorProvider>()
            ;

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
            .AddVariableDefinitionProvider<WorkflowVariablesVariableProvider>()
            ;

        // Handlers.
        services.AddNotificationHandlersFrom<JavaScriptFeature>();

        // Type Script definitions.
        services.AddFunctionDefinitionProvider<InputFunctionsDefinitionProvider>();

        // UI property handlers.
        services.AddScoped<IPropertyUIHandler, RunJavaScriptOptionsProvider>();
    }
}
