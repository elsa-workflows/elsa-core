using Elsa.Extensions;
using Elsa.Http.Models;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.TypeDefinitions.Builders;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Http.Scripting.JavaScript;

/// <summary>
/// Configures the JavaScript engine with additional .NET types that can be instantiated.
/// </summary>
[PublicAPI]
public class HttpJavaScriptHandler : INotificationHandler<EvaluatingJavaScript>, ITypeDefinitionProvider, IFunctionDefinitionProvider
{
    private readonly ITypeDescriber _typeDescriber;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpJavaScriptHandler"/> class.
    /// </summary>
    public HttpJavaScriptHandler(ITypeDescriber typeDescriber)
    {
        _typeDescriber = typeDescriber;
    }

    /// <inheritdoc />
    Task INotificationHandler<EvaluatingJavaScript>.HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        engine.RegisterType<HttpRequestHeaders>();
        engine.RegisterType<Downloadable>();

        var activityExecutionContext = notification.Context;

        engine.SetValue("createEventTriggerUrl", (Func<string, object?, string>)((eventName, lifetimeOrExpiryDate) =>
        {
            return lifetimeOrExpiryDate switch
            {
                TimeSpan lifetime => activityExecutionContext.GenerateEventTriggerUrl(eventName, lifetime),
                DateTimeOffset expiryDate => activityExecutionContext.GenerateEventTriggerUrl(eventName, expiryDate),
                _ => activityExecutionContext.GenerateEventTriggerUrl(eventName)
            };
        }));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    ValueTask<IEnumerable<TypeDefinition>> ITypeDefinitionProvider.GetTypeDefinitionsAsync(TypeDefinitionContext context)
    {
        var definitions = GetTypeDefinitions(context);
        return new(definitions);
    }

    /// <inheritdoc />
    ValueTask<IEnumerable<FunctionDefinition>> IFunctionDefinitionProvider.GetFunctionDefinitionsAsync(TypeDefinitionContext context)
    {
        var definitions = GetFunctionDefinitions(context);
        return new(definitions);
    }
    
    private IEnumerable<TypeDefinition> GetTypeDefinitions(TypeDefinitionContext context)
    {
        yield return _typeDescriber.DescribeType(typeof(HttpRequestHeaders));
        yield return _typeDescriber.DescribeType(typeof(Downloadable));
    }

    private IEnumerable<FunctionDefinition> GetFunctionDefinitions(TypeDefinitionContext context)
    {
        yield return CreateFunctionDefinition(function => function.Name("createEventTriggerUrl").ReturnType("string").Parameter("eventName", "string"));
        yield return CreateFunctionDefinition(function => function.Name("createEventTriggerUrl").ReturnType("string").Parameter("eventName", "string").Parameter("lifetime", "TimeSpan"));
        yield return CreateFunctionDefinition(function => function.Name("createEventTriggerUrl").ReturnType("string").Parameter("eventName", "string").Parameter("expiresAt", "DateTimeOffset"));
    }

    private FunctionDefinition CreateFunctionDefinition(Action<FunctionDefinitionBuilder> setup)
    {
        var builder = new FunctionDefinitionBuilder();
        setup(builder);
        return builder.BuildFunctionDefinition();
    }
}