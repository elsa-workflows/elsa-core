using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Scripting.Liquid.Messages;
using Fluid;
using MediatR;

namespace Elsa.Activities.Http.Handlers
{
    public class HttpLiquidHandler : INotificationHandler<EvaluatingLiquidExpression>
    {
        public Task Handle(EvaluatingLiquidExpression notification, CancellationToken cancellationToken)
        {
            var context = notification.TemplateContext;

            context.MemberAccessStrategy.Register<HttpRequestModel>();

            return Task.CompletedTask;
        }
    }
}