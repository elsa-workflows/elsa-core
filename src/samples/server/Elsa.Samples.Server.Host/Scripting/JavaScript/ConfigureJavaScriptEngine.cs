using System.Threading;
using System.Threading.Tasks;
using Elsa.Samples.Server.Host.Models;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.JavaScript.Messages;
using MediatR;

namespace Elsa.Samples.Server.Host.Scripting.JavaScript
{
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;
            
            engine.RegisterType<SomeMessage>();
            return Task.CompletedTask;
        }
    }
}