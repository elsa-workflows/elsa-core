using Elsa.Extensions;
using Elsa.Http.Models;
using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Contracts;
using JetBrains.Annotations;

namespace Elsa.Http.Scripting.JavaScript;

/// <summary>
/// Configures the JavaScript engine with workflow input getters.
/// </summary>
[PublicAPI]
public class HttpJavaScriptHandler : INotificationHandler<EvaluatingJavaScript>
{
    
    /// <inheritdoc />
    public async Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        var engine = notification.Engine;
        engine.RegisterType<HttpRequestHeaders>();
    }
}