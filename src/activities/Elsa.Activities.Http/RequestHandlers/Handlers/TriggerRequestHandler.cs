using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Indexes;
using Elsa.Activities.Http.RequestHandlers.Results;
using Elsa.Activities.Http.Services;
using Elsa.Extensions;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Open.Linq.AsyncExtensions;
using ISession = YesSql.ISession;

namespace Elsa.Activities.Http.RequestHandlers.Handlers
{
    public class TriggerRequestHandler : IRequestHandler
    {
        private readonly HttpContext _httpContext;
        private readonly IWorkflowHost _workflowHost;
        private readonly IWorkflowRegistry _registry;
        private readonly ISession _session;
        private readonly CancellationToken _cancellationToken;

        public TriggerRequestHandler(
            IHttpContextAccessor httpContext,
            IWorkflowHost workflowHost,
            IWorkflowRegistry registry,
            ISession session,
            ITokenSerializer serializer)
        {
            _httpContext = httpContext.HttpContext;
            _workflowHost = workflowHost;
            _registry = registry;
            _session = session;
            _cancellationToken = httpContext.HttpContext.RequestAborted;
        }

        public async Task<IRequestHandlerResult> HandleRequestAsync()
        {
            // TODO: Optimize this by building up a hash of routes and workflows to execute.
            var requestPath = _httpContext.Request.Path;
            var method = _httpContext.Request.Method;

            var httpWorkflows =
                await _registry.GetWorkflowsByStartActivityAsync<ReceiveHttpRequest>(_cancellationToken);

            var workflowsToStart = Filter(httpWorkflows, requestPath, method).ToList();

            var workflowsToResume =
                await _session
                    .QueryWorkflowInstances()
                    .With<WorkflowInstanceByReceiveHttpRequestIndex>()
                    .Where(x => x.RequestPath == requestPath && x.RequestMethod == null || x.RequestMethod == method)
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