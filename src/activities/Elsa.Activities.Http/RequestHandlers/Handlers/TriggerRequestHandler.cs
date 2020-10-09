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
        private readonly HttpContext _httpContext;
        private readonly IWorkflowHost _workflowHost;
        private readonly IWorkflowRegistry _registry;
        private readonly IWorkflowInstanceStore _workflowInstanceStore;
        private readonly ITokenSerializer _serializer;
        private readonly CancellationToken _cancellationToken;

        public TriggerRequestHandler(
            IHttpContextAccessor httpContext,
            IWorkflowHost workflowHost,
            IWorkflowRegistry registry,
            IWorkflowInstanceStore workflowInstanceStore,
            ITokenSerializer serializer)
        {
            this._httpContext = httpContext.HttpContext;
            this._workflowHost = workflowHost;
            this._registry = registry;
            this._workflowInstanceStore = workflowInstanceStore;
            this._serializer = serializer;
            _cancellationToken = httpContext.HttpContext.RequestAborted;
        }

        public async Task<IRequestHandlerResult> HandleRequestAsync()
        {
            // TODO: Optimize this by building up a hash of routes and workflows to execute.
            var requestPath = _httpContext.Request.Path;
            var method = _httpContext.Request.Method;
            var httpWorkflows = await _registry.GetWorkflowsByStartActivityAsync<ReceiveHttpRequest>(_cancellationToken);
            var workflowsToStart = Filter(httpWorkflows, requestPath, method).ToList();
            var suspendedWorkflows = await _workflowInstanceStore.ListByBlockingActivityAsync<ReceiveHttpRequest>(cancellationToken: _cancellationToken);

            //var workflowsToResume = Filter(suspendedWorkflows, requestPath, method).ToList();

            // if (!workflowsToStart.Any() && !workflowsToResume.Any())
            //     return new NextResult();
            //
            // await InvokeWorkflowsToStartAsync(workflowsToStart);
            // await InvokeWorkflowsToResumeAsync(workflowsToResume);

            return !_httpContext.Items.ContainsKey(WorkflowHttpResult.Instance)
                ? (IRequestHandlerResult)new AcceptedResult()
                : new EmptyResult();
        }

        // private IEnumerable<(WorkflowInstance WorkflowInstance, ActivityInstanceRecord BlockingActivity)> Filter(
        //     IEnumerable<(WorkflowInstance WorkflowInstance, ActivityInstanceRecord BlockingActivity)> items,
        //     PathString path,
        //     string method) =>
        //     items.Where(x => IsMatch(x.BlockingActivity, path, method));

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
                await _workflowHost.RunWorkflowAsync(
                    workflow,
                    activity.Id,
                    cancellationToken: _cancellationToken);
            }
        }

        private async Task InvokeWorkflowsToResumeAsync(IEnumerable<(WorkflowInstance, ActivityInstance)> items)
        {
            foreach (var (workflowInstance, activity) in items)
            {
                await _workflowHost.RunWorkflowInstanceAsync(
                    workflowInstance,
                    activity.Id,
                    cancellationToken: _cancellationToken);
            }
        }
    }
}