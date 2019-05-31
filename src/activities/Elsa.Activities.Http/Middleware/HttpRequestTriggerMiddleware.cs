using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Runtime;
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

        public async Task InvokeAsync(HttpContext context, IWorkflowHost workflowHost, IWorkflowDefinitionStore workflowDefinitionStore, IWorkflowInstanceStore workflowInstanceStore)
        {
            var requestPath = new Uri(context.Request.Path.ToString(), UriKind.Relative);
            var cancellationToken = context.RequestAborted;
            var workflowsToStart = await FilterByPath(workflowDefinitionStore.ListByStartActivityAsync<HttpRequestTrigger>(cancellationToken), requestPath).ToListAsync();
            var workflowsToResume = await FilterByPath(workflowInstanceStore.ListByBlockingActivityAsync<HttpRequestTrigger>(cancellationToken), requestPath).ToListAsync();

            if (!workflowsToStart.Any() && !workflowsToResume.Any())
            {
                await next(context);
            }
            else
            {
                await InvokeWorkflowsToStartAsync(workflowHost, workflowsToStart, cancellationToken);
                await InvokeWorkflowsToResumeAsync(workflowHost, workflowsToResume, cancellationToken);
            }
        }

        private async Task<IEnumerable<Tuple<Workflow, HttpRequestTrigger>>> FilterByPath(Task<IEnumerable<Tuple<Workflow, HttpRequestTrigger>>> items, Uri path)
        {
            return (await items).Where(x => x.Item2.Path == path);
        }

        private async Task InvokeWorkflowsToStartAsync(IWorkflowHost workflowHost, IEnumerable<Tuple<Workflow, HttpRequestTrigger>> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                await workflowHost.StartWorkflowAsync(item.Item1, item.Item2, Variables.Empty, cancellationToken);
            }
        }
        
        private async Task InvokeWorkflowsToResumeAsync(IWorkflowHost workflowHost, IEnumerable<Tuple<Workflow, HttpRequestTrigger>> items, CancellationToken cancellationToken)
        {
            foreach (var item in items)
            {
                await workflowHost.ResumeWorkflowAsync(item.Item1, item.Item2, Variables.Empty, cancellationToken);
            }
        }
    }
}