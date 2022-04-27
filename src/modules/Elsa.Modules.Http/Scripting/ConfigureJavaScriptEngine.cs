using System.Threading;
using System.Threading.Tasks;
using Elsa.Mediator.Services;
using Elsa.Scripting.JavaScript.Notifications;

namespace Elsa.Modules.Http.Scripting;

public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScript>
{
    public Task HandleAsync(EvaluatingJavaScript notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}