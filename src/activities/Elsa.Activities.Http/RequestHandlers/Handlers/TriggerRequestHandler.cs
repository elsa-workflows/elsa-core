using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Activities;
using Elsa.Activities.Http.RequestHandlers.Results;
using Elsa.Activities.Http.Services;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace Elsa.Activities.Http.RequestHandlers.Handlers
{
    public class TriggerRequestHandler : IRequestHandler
    {
        private readonly HttpContext httpContext;
        private readonly IWorkflowRunner workflowRunner;
        private readonly IWorkflowRegistry registry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly CancellationToken cancellationToken;

        public TriggerRequestHandler(
            IHttpContextAccessor httpContext,
            IWorkflowRunner workflowRunner,
            IWorkflowRegistry registry,
            IWorkflowInstanceStore workflowInstanceStore)
        {
            this.httpContext = httpContext.HttpContext;
            this.workflowRunner = workflowRunner;
            this.registry = registry;
            this.workflowInstanceStore = workflowInstanceStore;
            cancellationToken = httpContext.HttpContext.RequestAborted;
        }

        public async Task<IRequestHandlerResult> HandleRequestAsync()
        {
            // TODO: Optimize this by building up a hash of routes and workflows to execute.
            var requestPath = new Uri(httpContext.Request.Path.ToString(), UriKind.Relative);
            var method = httpContext.Request.Method;
            var httpWorkflows = await registry.ListByStartActivityAsync(nameof(ReceiveHttpRequest), cancellationToken);
            var workflowsToStart = Filter(httpWorkflows, requestPath, method).ToList();
            var haltedHttpWorkflows = await workflowInstanceStore.ListByBlockingActivityAsync<ReceiveHttpRequest>(
                cancellationToken: cancellationToken);

            var workflowsToResume = Filter(haltedHttpWorkflows, requestPath, method).ToList();

            if (!workflowsToStart.Any() && !workflowsToResume.Any())
                return new NextResult();

            await InvokeWorkflowsToStartAsync(workflowsToStart);
            await InvokeWorkflowsToResumeAsync(workflowsToResume);

            return !httpContext.Items.ContainsKey(WorkflowHttpResult.Instance)
                ? (IRequestHandlerResult)new AcceptedResult()
                : new EmptyResult();
        }

        private IEnumerable<(WorkflowInstance, ActivityInstance)> Filter(
            IEnumerable<(WorkflowInstance, ActivityInstance)> items,
            Uri path,
            string method)
        {
            return items.Where(x => IsMatch(x.Item2.State, path, method));
        }

        private IEnumerable<(WorkflowBlueprint, IActivity)> Filter(
            IEnumerable<(WorkflowBlueprint, IActivity)> items,
            Uri path,
            string method)
        {
            return items.Where(x => IsMatch(x.Item2.State, path, method));
        }

        private bool IsMatch(Variables state, Uri path, string method)
        {
            var m = ReceiveHttpRequest.GetMethod(state);
            var p = ReceiveHttpRequest.GetPath(state);
            return (string.IsNullOrWhiteSpace(m) || m == method) && p == path;
        }

        private async Task InvokeWorkflowsToStartAsync(
            IEnumerable<(WorkflowBlueprint, IActivity)> items)
        {
            foreach (var item in items)
            {
                await workflowRunner.RunAsync(
                    item.Item1,
                    Variables.Empty,
                    new[] { item.Item2.Id },
                    cancellationToken: cancellationToken);
            }
        }

        private async Task InvokeWorkflowsToResumeAsync(IEnumerable<(WorkflowInstance, ActivityInstance)> items)
        {
            foreach (var (workflowInstance, activity) in items)
            {
                await workflowRunner.ResumeAsync(
                    workflowInstance,
                    Variables.Empty,
                    new[] { activity.Id },
                    cancellationToken);
            }
        }
    }
}