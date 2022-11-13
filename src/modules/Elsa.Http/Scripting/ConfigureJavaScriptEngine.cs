using Elsa.JavaScript.Notifications;
using Elsa.Mediator.Services;

namespace Elsa.Http.Scripting;

public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScript>
{
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}