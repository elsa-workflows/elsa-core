using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Services;
using Elsa.Services.WorkflowStorage;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace Elsa.Activities.Email.Handlers
{
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;
            
            engine.RegisterType<EmailAttachment>();
            return Task.CompletedTask;
        }
    }
}