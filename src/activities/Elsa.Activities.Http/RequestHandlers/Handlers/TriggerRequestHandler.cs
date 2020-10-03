using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.RequestHandlers.Results;
using Elsa.Activities.Http.Services;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.RequestHandlers.Handlers
{
    public class TriggerRequestHandler : IRequestHandler
    {
        private readonly HttpContext httpContext;
        private readonly IWorkflowHost workflowHost;
        private readonly IWorkflowRegistry registry;
        private readonly IWorkflowInstanceStore workflowInstanceStore;
        private readonly ITokenSerializer serializer;
        private readonly CancellationToken cancellationToken;

        public TriggerRequestHandler(
            IHttpContextAccessor httpContext,
            IWorkflowHost workflowHost,
            IWorkflowRegistry registry,
            IWorkflowInstanceStore workflowInstanceStore,
            ITokenSerializer serializer)
        {
            this.httpContext = httpContext.HttpContext;
            this.workflowHost = workflowHost;
            this.registry = registry;
            this.workflowInstanceStore = workflowInstanceStore;
            this.serializer = serializer;
            cancellationToken = httpContext.HttpContext.RequestAborted;
        }

        public async Task<IRequestHandlerResult> HandleRequestAsync()
        {
            // TODO: Optimize this by building up a hash of routes and workflows to execute.
            var requestPath = httpContext.Request.Path;
            var method = httpContext.Request.Method;
            var httpWorkflows = await registry.GetWorkflowsByStartActivityAsync<ReceiveHttpRequest>(cancellationToken);
            var workflowsToStart = Filter(httpWorkflows, requestPath, method).ToList();
            var suspendedWorkflows = await workflowInstanceStore.ListByBlockingActivityAsync<ReceiveHttpRequest>(cancellationToken: cancellationToken);

            var workflowsToResume = Filter(suspendedWorkflows, requestPath, method).ToList();

            if (!workflowsToStart.Any() && !workflowsToResume.Any())
                return new NextResult();

            await InvokeWorkflowsToStartAsync(workflowsToStart);
            await InvokeWorkflowsToResumeAsync(workflowsToResume);

            return !httpContext.Items.ContainsKey(WorkflowHttpResult.Instance)
                ? (IRequestHandlerResult)new AcceptedResult()
                : new EmptyResult();
        }

        private IEnumerable<(WorkflowInstance WorkflowInstance, ActivityInstanceRecord BlockingActivity)> Filter(
            IEnumerable<(WorkflowInstance WorkflowInstance, ActivityInstanceRecord BlockingActivity)> items,
            PathString path,
            string method) =>
            items.Where(x => IsMatch(x.BlockingActivity, path, method));

        private IEnumerable<(Workflow Workflow, ReceiveHttpRequest Activity)> Filter(
            IEnumerable<(Workflow Workflow, ReceiveHttpRequest Activity)> items,
            PathString path,
            string method) =>
            items.Where(x => IsMatch(x.Activity, path, method));

        private bool IsMatch(ReceiveHttpRequest activity, PathString path, string method)
        {
            var m = activity.Method;
            var p = activity.Path;
            return (string.IsNullOrWhiteSpace(m) || m == method) && p == path;
        }

        private async Task InvokeWorkflowsToStartAsync(
            IEnumerable<(Workflow Workflow, ReceiveHttpRequest Activity)> items)
        {
            foreach (var (workflow, activity) in items)
            {
                await workflowHost.RunWorkflowAsync(
                    workflow,
                    activity.Id,
                    cancellationToken: cancellationToken);
            }
        }

        private async Task InvokeWorkflowsToResumeAsync(IEnumerable<(WorkflowInstance, ActivityInstanceRecord)> items)
        {
            foreach (var (workflowInstance, activity) in items)
            {
                await workflowHost.RunWorkflowInstanceAsync(
                    workflowInstance,
                    activity.Id,
                    cancellationToken: cancellationToken);
            }
        }
    }
}