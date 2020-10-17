using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Indexes;
using Elsa.Activities.Http.RequestHandlers.Results;
using Elsa.Activities.Http.Services;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Http.RequestHandlers.Handlers
{
    public class TriggerRequestHandler : IRequestHandler
    {
        private readonly HttpContext _httpContext;
        private readonly IWorkflowHost _workflowHost;
        private readonly IWorkflowRegistry _registry;
        private readonly IWorkflowInstanceManager _workflowInstanceManager;
        private readonly CancellationToken _cancellationToken;

        public TriggerRequestHandler(
            IHttpContextAccessor httpContext,
            IWorkflowHost workflowHost,
            IWorkflowRegistry registry,
            IWorkflowInstanceManager workflowInstanceManager)
        {
            _httpContext = httpContext.HttpContext;
            _workflowHost = workflowHost;
            _registry = registry;
            _workflowInstanceManager = workflowInstanceManager;
            _cancellationToken = httpContext.HttpContext.RequestAborted;
        }

        public async Task<IRequestHandlerResult> HandleRequestAsync()
        {
            // TODO: Optimize this by building up a hash of routes and workflows to execute.
            var requestPath = _httpContext.Request.Path.ToString();
            var method = _httpContext.Request.Method;

            var httpWorkflows =
                await _registry.GetWorkflowsByStartActivityAsync<ReceiveHttpRequest>(_cancellationToken);

            var workflowsToStart = httpWorkflows;

            var workflowsToResume =
                await _workflowInstanceManager
                    .Query<WorkflowInstanceByReceiveHttpRequestIndex>(
                        x => x.RequestPath == requestPath && x.RequestMethod == null || x.RequestMethod == method)
                    .ListAsync()
                    .ToList();

            if (!workflowsToStart.Any() && !workflowsToResume.Any())
                return new NextResult();

            var workflowsToResumeTuples =
                from workflowInstance in workflowsToResume
                from blockingActivity in workflowInstance.BlockingActivities
                where blockingActivity.ActivityType == nameof(ReceiveHttpRequest)
                from activity in workflowInstance.Activities
                where activity.Id == blockingActivity.ActivityId
                where activity.Data.Value<string>(nameof(ReceiveHttpRequest.Path)) == requestPath
                where activity.Data.Value<string>(nameof(ReceiveHttpRequest.Method)) == method
                select (workflowInstance, activity);

            await InvokeWorkflowsToStartAsync(workflowsToStart);
            await InvokeWorkflowsToResumeAsync(workflowsToResumeTuples);

            return !_httpContext.Items.ContainsKey(WorkflowHttpResult.Instance)
                ? (IRequestHandlerResult)new AcceptedResult()
                : new EmptyResult();
        }

        private async Task InvokeWorkflowsToStartAsync(
            IEnumerable<(IWorkflowBlueprint Workflow, IActivityBlueprint Activity)> items)
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
                await _workflowHost.RunWorkflowAsync(
                    workflowInstance,
                    activity.Id,
                    cancellationToken: _cancellationToken);
            }
        }
    }
}