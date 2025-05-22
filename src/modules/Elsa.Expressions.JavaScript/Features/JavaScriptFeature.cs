using Elsa.Caching.Features;
using Elsa.Common.Features;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Expressions.JavaScript.Activities;
using Elsa.Expressions.JavaScript.Contracts;
using Elsa.Expressions.JavaScript.Extensions;
using Elsa.Expressions.JavaScript.HostedServices;
using Elsa.Expressions.JavaScript.Options;
using Elsa.Expressions.JavaScript.Providers;
using Elsa.Expressions.JavaScript.Services;
using Elsa.Expressions.JavaScript.TypeDefinitions.Contracts;
using Elsa.Expressions.JavaScript.TypeDefinitions.Services;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Expressions.JavaScript.Features;

/// <summary>
/// Installs JavaScript integration.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
[DependsOn(typeof(MemoryCacheFeature))]
public class JavaScriptFeature : FeatureBase
{
    /// <inheritdoc />
    public JavaScriptFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// Configures the Jint options.
    /// </summary>
    private Action<JintOptions> JintOptions { get; set; } = _ => { };

    public JavaScriptFeature ConfigureJintOptions(Action<JintOptions> configure)
    {
        JintOptions += configure;
        return this;
    }

    /// <inheritdoc />
    public override void ConfigureHostedServices()
    {
        ConfigureHostedService<RegisterVariableTypesWithJavaScriptHostedService>();
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.AddFastEndpointsAssembly<JavaScriptFeature>();
    }

    /// <inheritdoc />
    public override void Apply()
    {
        Services.Configure(JintOptions);

        // JavaScript services.
        Services
            .AddScoped<IJavaScriptEvaluator, JintJavaScriptEvaluator>()
            .AddScoped<ITypeDefinitionService, TypeDefinitionService>()
            .AddExpressionDescriptorProvider<JavaScriptExpressionDescriptorProvider>()
            ;

        // Type definition services.
        Services
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
        Services.AddNotificationHandlersFrom<JavaScriptFeature>();

        // Activities.
        Module.UseWorkflowManagement(management => management.AddActivity<RunJavaScript>());

        // Type Script definitions.
        Services.AddFunctionDefinitionProvider<InputFunctionsDefinitionProvider>();

        // UI property handlers.
        Services.AddScoped<IPropertyUIHandler, RunJavaScriptOptionsProvider>();
    }
}