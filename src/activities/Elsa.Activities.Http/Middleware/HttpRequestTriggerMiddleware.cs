using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Esprima.Ast;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Middleware
{
    public class HttpRequestTriggerMiddleware
    {
        private readonly RequestDelegate next;

        public HttpRequestTriggerMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(
            HttpContext context, 
            IWorkflowInvoker workflowInvoker,
            IWorkflowRegistry registry, 
            IWorkflowInstanceStore workflowInstanceStore)
        {
            var requestPath = new Uri(context.Request.Path.ToString(), UriKind.Relative);
            var cancellationToken = context.RequestAborted;
            var workflowsToStart = FilterByPath(registry.ListByStartActivity(nameof(HttpRequestTrigger)), requestPath).ToList();
            var workflowsToResume = FilterByPath(await workflowInstanceStore.ListByBlockingActivityAsync<HttpRequestTrigger>(cancellationToken), requestPath).ToList();

            if (!workflowsToStart.Any() && !workflowsToResume.Any())
            {
                await next(context);
            }
            else
            {
                await InvokeWorkflowsToStartAsync(workflowInvoker, workflowsToStart, cancellationToken);
                await InvokeWorkflowsToResumeAsync(workflowInvoker, workflowsToResume, cancellationToken);
            }
        }

        private IEnumerable<(WorkflowDefinition, ActivityDefinition)> FilterByPath(IEnumerable<(WorkflowDefinition, ActivityDefinition)> items, Uri path)
        {
            return items.Where(x => x.Item2.State.GetState<Uri>(nameof(HttpRequestTrigger.Path)) == path);
        }
        
        private IEnumerable<(WorkflowInstance, ActivityInstance)> FilterByPath(IEnumerable<(WorkflowInstance, ActivityInstance)> items, Uri path)
        {
            return items.Where(x => x.Item2.State.GetState<Uri>(nameof(HttpRequestTrigger.Path)) == path);
        }

        private async Task InvokeWorkflowsToStartAsync(IWorkflowInvoker workflowInvoker, IEnumerable<(WorkflowDefinition, ActivityDefinition)> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                await workflowInvoker.InvokeAsync(item.Item1, Variables.Empty, startActivityIds: new[] { item.Item2.Id }, cancellationToken: cancellationToken);
            }
        }
        
        private async Task InvokeWorkflowsToResumeAsync(IWorkflowInvoker workflowInvoker, IEnumerable<(WorkflowInstance, ActivityInstance)> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                var workflowInstance = item.Item1;

                workflowInstance.Status = WorkflowStatus.Resuming;
                await workflowInvoker.InvokeAsync(workflowInstance, Variables.Empty, new[]{item.Item2.Id}, cancellationToken);
            }
        }
    }
}