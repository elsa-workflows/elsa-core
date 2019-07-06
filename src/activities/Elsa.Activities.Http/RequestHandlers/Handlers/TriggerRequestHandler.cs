using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Http.RequestHandlers.Results;
using Elsa.Activities.Http.Services;
using Elsa.Core.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization.Models;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Elsa.Activities.Http.RequestHandlers.Handlers
{
    public class TriggerRequestHandler : IRequestHandler
    {
        private readonly HttpContext httpContext;
        private readonly IWorkflowInvoker workflowInvoker;
        private readonly IWorkflowRegistry registry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly CancellationToken cancellationToken;

        public TriggerRequestHandler(HttpContext httpContext, 
            IWorkflowInvoker workflowInvoker,
            IWorkflowRegistry registry, 
            IWorkflowInstanceStore workflowInstanceStore)
        {
            this.httpContext = httpContext;
            this.workflowInvoker = workflowInvoker;
            this.registry = registry;
            this.workflowInstanceStore = workflowInstanceStore;
            this.cancellationToken = httpContext.RequestAborted;
        }

        public async Task<IRequestHandlerResult> HandleRequestAsync()
        {
            // TODO: Optimize this by building up a hash of routes and workflows to execute.
            var requestPath = new Uri(httpContext.Request.Path.ToString(), UriKind.Relative);
            var method = httpContext.Request.Method;
            var workflowsToStart = Filter(registry.ListByStartActivity(nameof(HttpRequestTrigger)), requestPath, method).ToList();
            var workflowsToResume = Filter(await workflowInstanceStore.ListByBlockingActivityAsync<HttpRequestTrigger>(cancellationToken), requestPath, method).ToList();

            if (!workflowsToStart.Any() && !workflowsToResume.Any())
            {
                return new NextResult();
            }

            await InvokeWorkflowsToStartAsync(workflowsToStart);
            await InvokeWorkflowsToResumeAsync(workflowsToResume);
            
            return !httpContext.Items.ContainsKey(WorkflowHttpResult.Instance) 
                ? (IRequestHandlerResult) new AcceptedResult()
                : new EmptyResult();
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

        private async Task InvokeWorkflowsToStartAsync(IEnumerable<(WorkflowDefinition, ActivityDefinition)> items)
        {
            foreach (var item in items)
            {
                await workflowInvoker.InvokeAsync(item.Item1, Variables.Empty, startActivityIds: new[] { item.Item2.Id }, cancellationToken: cancellationToken);
            }
        }
        
        private async Task InvokeWorkflowsToResumeAsync(IEnumerable<(WorkflowInstance, ActivityInstance)> items)
        {
            foreach (var (workflowInstance, activity) in items)
            {
                await workflowInvoker.ResumeAsync(workflowInstance, Variables.Empty, new[]{activity.Id}, cancellationToken);
            }
        }
    }
}