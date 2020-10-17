using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Services.Models;
using Elsa.Triggers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Activities.Http.Triggers
{
    public class ReceiveHttpRequestTrigger : ITrigger
    {
        public PathString Path { get; set; }
        public string? Method { get; set; }
        public string ActivityId { get; set; }
    }

    public class ReceiveHttpRequestTriggerProvider : TriggerProvider<ReceiveHttpRequestTrigger>
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IWorkflowFactory _workflowFactory;

        public ReceiveHttpRequestTriggerProvider(IServiceProvider serviceProvider, IWorkflowFactory workflowFactory)
        {
            _serviceProvider = serviceProvider;
            _workflowFactory = workflowFactory;
        }

        public override async IAsyncEnumerable<ITrigger> GetTriggersAsync(IWorkflowBlueprint workflowBlueprint, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var httpRequestActivities = workflowBlueprint
                .GetStartActivities()
                .Where(x => x.Type == nameof(ReceiveHttpRequest))
                .ToList();

            using var scope = _serviceProvider.CreateScope();
            var workflowInstance = await _workflowFactory.InstantiateAsync(workflowBlueprint, cancellationToken: cancellationToken);
            var workflowExecutionContext = new WorkflowExecutionContext(scope.ServiceProvider, workflowBlueprint, workflowInstance);

            foreach (var activity in httpRequestActivities)
            {
                var activityExecutionContext = new ActivityExecutionContext(workflowExecutionContext, scope.ServiceProvider, activity);

                yield return new ReceiveHttpRequestTrigger
                {
                    ActivityId = activity.Id,
                    Path = await workflowBlueprint.GetActivityPropertyValue<ReceiveHttpRequest, PathString>(activity.Id, x => x.Path, activityExecutionContext, cancellationToken),
                    Method = await workflowBlueprint.GetActivityPropertyValue<ReceiveHttpRequest, string?>(activity.Id, x => x.Method, activityExecutionContext, cancellationToken)
                };
            }
        }
    }
}