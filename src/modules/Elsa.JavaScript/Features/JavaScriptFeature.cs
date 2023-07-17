using Elsa.Common.Features;
using Elsa.Expressions.Contracts;
using Elsa.Expressions.Features;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.Expressions;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Providers;
using Elsa.JavaScript.Services;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Providers;
using Elsa.JavaScript.TypeDefinitions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.JavaScript.Features;

/// <summary>
/// Installs JavaScript integration.
/// </summary>
[DependsOn(typeof(MediatorFeature))]
[DependsOn(typeof(ExpressionsFeature))]
public class JavaScriptFeature : FeatureBase
{
    /// <inheritdoc />
    public JavaScriptFeature(IModule module) : base(module)
    {
    }

    /// <inheritdoc />
    public override void Apply()
    {
        // JavaScript services.
        Services
            .AddSingleton<IExpressionSyntaxProvider, JavaScriptExpressionSyntaxProvider>()
            .AddSingleton<IJavaScriptEvaluator, JintJavaScriptEvaluator>()
            .AddSingleton<ITypeDefinitionService, TypeDefinitionService>()
            .AddExpressionHandler<JavaScriptExpressionHandler, JavaScriptExpression>()
            ;

        // Type definition services.
        Services
            .AddSingleton<ITypeDefinitionService, TypeDefinitionService>()
            .AddSingleton<ITypeDescriber, TypeDescriber>()
            .AddSingleton<ITypeDefinitionDocumentRenderer, TypeDefinitionDocumentRenderer>()
            .AddSingleton<ITypeAliasRegistry, TypeAliasRegistry>()
            .AddFunctionDefinitionProvider<CommonFunctionsDefinitionProvider>()
            .AddFunctionDefinitionProvider<ActivityOutputFunctionsDefinitionProvider>()
            .AddTypeDefinitionProvider<CommonTypeDefinitionProvider>()
            .AddTypeDefinitionProvider<VariableTypeDefinitionProvider>()
            ;
        
        // Handlers.
        Services.AddNotificationHandlersFrom<JavaScriptFeature>();
    }
}