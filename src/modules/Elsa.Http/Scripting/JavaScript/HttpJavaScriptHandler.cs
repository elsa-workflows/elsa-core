using Elsa.Extensions;
using Elsa.Http.Models;
using Elsa.JavaScript.Notifications;
using Elsa.JavaScript.TypeDefinitions.Contracts;
using Elsa.JavaScript.TypeDefinitions.Models;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Http.Scripting.JavaScript;

/// <summary>
/// Configures the JavaScript engine with additional .NET types that can be instantiated.
/// </summary>
[PublicAPI]
public class HttpJavaScriptHandler : INotificationHandler<EvaluatingJavaScript>, ITypeDefinitionProvider
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
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        engine.RegisterType<HttpRequestHeaders>();
        engine.RegisterType<Downloadable>();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<TypeDefinition>> GetTypeDefinitionsAsync(TypeDefinitionContext context)
    {
        var definitions = GetTypeDefinitions(context);
        return new(definitions);
    }

    private IEnumerable<TypeDefinition> GetTypeDefinitions(TypeDefinitionContext context)
    {
        yield return _typeDescriber.DescribeType(typeof(HttpRequestHeaders));
        yield return _typeDescriber.DescribeType(typeof(Downloadable));
    }
}