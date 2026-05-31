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
using Elsa.Expressions.Options;
using Elsa.Extensions;
using Elsa.PackageManifest.Generator.Hints;
using Elsa.Workflows;
using Elsa.Workflows.Options;
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
    /// Enables access to any .NET class. Do not enable if you are executing workflows from untrusted sources (e.g. user defined workflows).
    ///
    /// See Jint docs for more: https://github.com/sebastienros/jint#accessing-net-assemblies-and-classes
    /// </summary>
    [ManifestSetting(
        DisplayName = "Allow CLR Access",
        Description = "Allow JavaScript expressions to access .NET classes.",
        Category = "Security",
        DefaultValue = "false",
        Advanced = true,
        RestartRequired = true)]
    public bool AllowClrAccess { get; set; }

    /// <summary>
    /// Enables access to .NET configuration via the <c>getConfig</c> function.
    /// Do not enable if you are executing workflows from untrusted sources (e.g user defined workflows).
    /// </summary>
    [ManifestSetting(
        DisplayName = "Allow Configuration Access",
        Description = "Allow JavaScript expressions to access application configuration through getConfig.",
        Category = "Security",
        DefaultValue = "false",
        Advanced = true,
        RestartRequired = true)]
    public bool AllowConfigurationAccess { get; set; }

    /// <summary>
    /// The timeout for script caching.
    /// </summary>
    /// <remarks>
    /// The <c>ScriptCacheTimeout</c> property specifies the duration for which the scripts are cached in the Jint JavaScript engine. When a script is executed, it is compiled and cached for future use. This caching improves performance by avoiding repetitive compilation of the same script.
    /// If the value of <c>ScriptCacheTimeout</c> is <c>null</c>, the scripts are cached indefinitely. If a time value is specified, the scripts will be purged from the cache after they've been unused for the specified duration and recompiled on next use.
    /// </remarks>
    [ManifestSetting(
        DisplayName = "Script Cache Timeout",
        Description = "Duration for which compiled JavaScript expressions stay cached.",
        Category = "Performance",
        DefaultValue = "1.00:00:00",
        RestartRequired = true)]
    public TimeSpan? ScriptCacheTimeout { get; set; } = TimeSpan.FromDays(1);

    /// <summary>
    /// Disables the generation of variable wrappers. E.g. <c>getMyVariable()</c> will no longer be available for variables. Instead, you can only access variables using <c>getVariable("MyVariable")</c> function.
    /// This is useful if your application requires the use of invalid JavaScript variable names.
    /// </summary>
    [ManifestSetting(
        DisplayName = "Disable Variable Wrappers",
        Description = "Disable generated JavaScript variable wrapper functions.",
        Category = "Runtime",
        DefaultValue = "false",
        Advanced = true,
        RestartRequired = true)]
    public bool DisableWrappers { get; set; }
    
    /// <summary>
    /// Disables copying workflow variables into the Jint engine and copying them back into the workflow execution context.
    /// Disabling this option will increase performance but will also prevent you from accessing workflow variables from within JavaScript expressions using the <c>variables.MyVariable</c> syntax.
    /// </summary>
    [ManifestSetting(
        DisplayName = "Disable Variable Copying",
        Description = "Disable copying workflow variables into and out of the JavaScript engine.",
        Category = "Performance",
        DefaultValue = "false",
        Advanced = true,
        RestartRequired = true)]
    public bool DisableVariableCopying { get; set; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.Configure<JintOptions>(options =>
        {
            options.AllowClrAccess = AllowClrAccess;
            options.AllowConfigurationAccess = AllowConfigurationAccess;
            options.ScriptCacheTimeout = ScriptCacheTimeout;
            options.DisableWrappers = DisableWrappers;
            options.DisableVariableCopying = DisableVariableCopying;
        });
        services.Configure<ExpressionOptions>(JavaScriptExceptionTypeAliasRegistrar.Register);
        services.Configure<WorkflowJsonTypeOptions>(JavaScriptExceptionTypeAliasRegistrar.Register);

        // JavaScript services.
        services
            .AddScoped<IJavaScriptEvaluator, JintJavaScriptEvaluator>()
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
