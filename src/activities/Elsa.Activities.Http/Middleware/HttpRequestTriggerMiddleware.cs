using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Core.Expressions;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Esprima.Ast;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

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
            var method = context.Request.Method;
            var cancellationToken = context.RequestAborted;
            var workflowsToStart = Filter(registry.ListByStartActivity(nameof(HttpRequestTrigger)), requestPath, method).ToList();
            var workflowsToResume = Filter(await workflowInstanceStore.ListByBlockingActivityAsync<HttpRequestTrigger>(cancellationToken), requestPath, method).ToList();

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
        
        private IEnumerable<(WorkflowInstance, ActivityInstance)> Filter(IEnumerable<(WorkflowInstance, ActivityInstance)> items, Uri path, string method)
        {
            return items.Where(x => IsMatch(x.Item2.State, path, method));
        }
        
        private IEnumerable<(WorkflowDefinition, ActivityDefinition)> Filter(IEnumerable<(WorkflowDefinition, ActivityDefinition)> items, Uri path, string method)
        {
            return items.Where(x => IsMatch(x.Item2.State, path, method));
        }
        
        private bool IsMatch(JObject state, Uri path, string method)
        {
            var m = HttpRequestTrigger.GetMethod(state);
            var p = HttpRequestTrigger.GetPath(state);
            return (string.IsNullOrWhiteSpace(m) || m == method) && p == path;
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