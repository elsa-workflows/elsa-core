using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Triggers
{
    public class ReceiveHttpRequestTrigger : Trigger
    {
        public PathString Path { get; set; }
        public string? Method { get; set; }
    }

    public class ReceiveHttpRequestTriggerProvider : TriggerProvider<ReceiveHttpRequestTrigger, ReceiveHttpRequest>
    {
        public override async ValueTask<ITrigger> GetTriggerAsync(TriggerProviderContext<ReceiveHttpRequest> context, CancellationToken cancellationToken) =>
            new ReceiveHttpRequestTrigger
            {
                Path = await context.Activity.GetPropertyValueAsync(x => x.Path, cancellationToken),
                Method = await context.Activity.GetPropertyValueAsync(x => x.Method, cancellationToken)
            };
    }
}