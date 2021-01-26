using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Triggers;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Triggers
{
    public class HttpRequestReceivedTrigger : ITrigger
    {
        public PathString Path { get; set; }
        public string? Method { get; set; }
        public string? CorrelationId { get; set; }
    }

    public class HttpRequestReceivedTriggerProvider : WorkflowTriggerProvider<HttpRequestReceivedTrigger, HttpRequestReceived>
    {
        public override async ValueTask<IEnumerable<ITrigger>> GetTriggersAsync(TriggerProviderContext<HttpRequestReceived> context, CancellationToken cancellationToken)
        {
            var path = ToLower(await context.Activity.GetPropertyValueAsync(x => x.Path, cancellationToken));
            var method = ToLower(await context.Activity.GetPropertyValueAsync(x => x.Method, cancellationToken));
            
            return new[]
            {
                new HttpRequestReceivedTrigger
                {
                    Path = path,
                    Method = method,
                    CorrelationId = ToLower(context.ActivityExecutionContext.WorkflowExecutionContext.CorrelationId)
                }
            };
        }

        private string? ToLower(string? s) => s?.ToLowerInvariant();
    }
}