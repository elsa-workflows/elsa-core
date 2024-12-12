using Elsa.Caching.Features;
using Elsa.Common.Features;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.HostedServices;
using Elsa.JavaScript.Options;
using Elsa.JavaScript.Providers;
using Elsa.JavaScript.Services;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Services;
using Elsa.Workflows.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.Features;

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
    public Action<JintOptions> JintOptions { get; set; } = _ => { };

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
            .AddTypeDefinitionProvider<CommonTypeDefinitionProvider>()
            .AddTypeDefinitionProvider<VariableTypeDefinitionProvider>()
            ;

        // Handlers.
        Services.AddNotificationHandlersFrom<JavaScriptFeature>();

        // Activities.
        Module.UseWorkflowManagement(management => management.AddActivity<RunJavaScript>());

        Services
            .AddScoped<IPropertyUIHandler, RunJavaScriptOptionsProvider>()
            .AddFunctionDefinitionProvider<InputFunctionsDefinitionProvider>();
    }
}