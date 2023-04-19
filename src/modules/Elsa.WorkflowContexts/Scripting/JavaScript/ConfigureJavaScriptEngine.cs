using Elsa.Extensions;
using Elsa.JavaScript.Contracts;
using Elsa.JavaScript.Extensions;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.TypeDefinitions.Builders;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Mediator.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Entities;

namespace Elsa.WorkflowContexts.Scripting.JavaScript;

/// <summary>
/// Configures the JavaScript engine with functions that allow access to workflow contexts.
/// </summary>
public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScript>, IFunctionDefinitionProvider, ITypeDefinitionProvider
{
    private readonly ITypeAliasRegistry _typeAliasRegistry;
    private readonly ITypeDescriber _typeDescriber;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureJavaScriptEngine"/> class.
    /// </summary>
    public ConfigureJavaScriptEngine(ITypeAliasRegistry typeAliasRegistry, ITypeDescriber typeDescriber)
    {
        _typeAliasRegistry = typeAliasRegistry;
        _typeDescriber = typeDescriber;
    }

    /// <inheritdoc />
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        if (!notification.Context.TryGetWorkflowExecutionContext(out var workflowExecutionContext))
            return Task.CompletedTask;

        var providerTypes = GetProviderTypes(workflowExecutionContext);
        var engine = notification.Engine;

        foreach (var providerType in providerTypes)
        {
            var providerName = providerType.GetProviderName();
            var functionName = $"get{providerName}";
            engine.SetValue(functionName, (Func<object?>)(() => workflowExecutionContext.GetWorkflowContext(providerType)));
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<TypeDefinition>> GetTypeDefinitionsAsync(TypeDefinitionContext context)
    {
        var providerTypes = GetProviderTypes(context.WorkflowDefinition);
        var contextTypes = providerTypes.Select(x => x.GetWorkflowContextType());
        var typeDefinitions = contextTypes.Select(x => _typeDescriber.DescribeType(x));
        return new(typeDefinitions);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<FunctionDefinition>> GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        var providerTypes = GetProviderTypes(context.WorkflowDefinition);
        var functionDefinitions = BuildFunctionDefinitions(providerTypes);
        return new(functionDefinitions);
    }

    private IEnumerable<FunctionDefinition> BuildFunctionDefinitions(IEnumerable<Type> providerTypes)
    {
        foreach (var providerType in providerTypes)
        {
            var builder = new FunctionDefinitionBuilder();
            var providerName = providerType.GetProviderName();
            var functionName = $"get{providerName}";
            var contextType = providerType.GetWorkflowContextType();
            var contextTypeName = _typeAliasRegistry.GetAliasOrDefault(contextType, contextType.Name);
            builder.Name(functionName).ReturnType(contextTypeName);

            yield return builder.BuildFunctionDefinition();
        }
    }

    private IEnumerable<Type> GetProviderTypes(WorkflowExecutionContext workflowExecutionContext) => workflowExecutionContext.Workflow.GetWorkflowContextProviderTypes();
    private IEnumerable<Type> GetProviderTypes(WorkflowDefinition workflowDefinition) => workflowDefinition.GetWorkflowContextProviderTypes();
}