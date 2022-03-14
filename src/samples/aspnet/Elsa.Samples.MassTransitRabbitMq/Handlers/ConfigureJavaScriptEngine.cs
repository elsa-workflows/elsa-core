using System.Threading;
using System.Threading.Tasks;
using Elsa.Samples.MassTransitRabbitMq.Messages;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.JavaScript.Messages;
using MediatR;

namespace Elsa.Samples.MassTransitRabbitMq.Handlers
{
    /// <summary>
    /// Configure the JS engine to allow instantiation of message classes.
    /// </summary>
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;
            
            engine.RegisterType<FirstMessage>();
            engine.RegisterType<SecondMessage>();
            return Task.CompletedTask;
        }
    }
}