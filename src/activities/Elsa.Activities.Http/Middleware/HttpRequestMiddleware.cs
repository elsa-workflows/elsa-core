using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Extensions;
using Elsa.Bookmarks;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Serialization;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Http.Middleware
{
    public class ReceiveHttpRequestMiddleware
    {
        // TODO: Figure out how to start jobs across multiple tenants / how to get a list of all tenants. 
        private const string TenantId = default;
        private readonly RequestDelegate _next;

        public ReceiveHttpRequestMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext httpContext,
            IWorkflowRegistry workflowRegistry,
            IBookmarkFinder bookmarkFinder,
            IWorkflowRunner workflowRunner,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowBlueprintReflector workflowBlueprintReflector,
            IContentSerializer contentSerializer)
        {
            var workflowInstance = await RunWorkflow(httpContext, workflowRegistry, bookmarkFinder, workflowRunner, workflowInstanceStore, workflowBlueprintReflector);

            if (workflowInstance == null)
            {
                httpContext.Response.StatusCode = (int) HttpStatusCode.NotFound;
                return;
            }

            if (workflowInstance.WorkflowStatus == WorkflowStatus.Faulted)
            {
                httpContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
                httpContext.Response.ContentType = "application/json";
                
                var model = new
                {
                    WorkflowInstanceId = workflowInstance.Id,
                    WorkflowStatus = workflowInstance.WorkflowStatus,
                    Fault = workflowInstance.Fault
                };
                
                await httpContext.Response.WriteAsync(contentSerializer.Serialize(model));
            }
        }
        
        private async Task<WorkflowInstance?> RunWorkflow(
            HttpContext httpContext,
            IWorkflowRegistry workflowRegistry,
            IBookmarkFinder bookmarkFinder,
            IWorkflowRunner workflowRunner,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowBlueprintReflector workflowBlueprintReflector)
        {
            var path = httpContext.Request.Path;
            var method = httpContext.Request.Method!;
            var cancellationToken = httpContext.RequestAborted;
            var serviceProvider = httpContext.RequestServices;
            httpContext.Request.TryGetCorrelationId(out var correlationId);

            // Find workflow definitions starting with an HttpRequestReceived activity and a matching Path and Method.
            var definitions = await FindHttpWorkflowBlueprints(workflowRegistry, workflowBlueprintReflector, serviceProvider, cancellationToken);
            var matchingDefinitions = definitions.Where(definition => AreSame(definition.Path, path) && (string.IsNullOrWhiteSpace(definition.Method) || AreSame(definition.Method!, method))).ToList();

            if (matchingDefinitions.Count > 1)
            {
                httpContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                await httpContext.Response.WriteAsync("Request matches multiple workflows.", cancellationToken);
                return null;
            }

            if (matchingDefinitions.Count == 1)
            {
                var definition = matchingDefinitions.First();
                var workflowBlueprint = definition.WorkflowBlueprint;
                var activityId = definition.ActivityBlueprint.Id;
                return await workflowRunner.RunWorkflowAsync(workflowBlueprint, activityId, cancellationToken: cancellationToken);
            }

            // Find workflow instances blocked on an HttpRequestReceived activity and a matching Path and Method.
            var triggerWithMethod = new HttpRequestReceivedBookmark
            {
                Path = ((string) path).ToLowerInvariant(),
                Method = method.ToLowerInvariant(),
                CorrelationId = correlationId?.ToLowerInvariant()
            };

            var triggerWithoutMethod = new HttpRequestReceivedBookmark
            {
                Path = ((string) path).ToLowerInvariant(),
                CorrelationId = correlationId?.ToLowerInvariant()
            };

            var triggers = new[] { triggerWithMethod, triggerWithoutMethod };
            var results = await bookmarkFinder.FindBookmarksAsync<HttpRequestReceived>(triggers, TenantId, cancellationToken).ToList();

            if (!results.Any())
            {
                await _next(httpContext);
                return null;
            }

            if (results.Count > 1)
            {
                httpContext.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                await httpContext.Response.WriteAsync("Request matches multiple workflows.", cancellationToken);
                return null;
            }

            var result = results.First();
            var workflowInstance = await workflowInstanceStore.FindByIdAsync(result.WorkflowInstanceId, cancellationToken);

            if (workflowInstance == null)
                return null;

            return await workflowRunner.RunWorkflowAsync(workflowInstance, result.ActivityId, cancellationToken: cancellationToken);
        }

        private async Task<IEnumerable<(IWorkflowBlueprint WorkflowBlueprint, IActivityBlueprint ActivityBlueprint, PathString Path, string? Method)>> FindHttpWorkflowBlueprints(
            IWorkflowRegistry workflowRegistry,
            IWorkflowBlueprintReflector workflowBlueprintReflector,
            IServiceProvider serviceProvider,
            CancellationToken cancellationToken)
        {
            // Find workflows starting with HttpRequestReceived.
            var workflows = await workflowRegistry.GetWorkflowsAsync(cancellationToken).ToListAsync(cancellationToken);

            var httpWorkflows =
                from workflow in workflows
                where workflow.TenantId == TenantId
                from activity in workflow.GetStartActivities<HttpRequestReceived>()
                select (workflow, activity);

            var matches = new List<(IWorkflowBlueprint, IActivityBlueprint, PathString, string?)>();

            foreach (var httpWorkflow in httpWorkflows)
            {
                var workflow = httpWorkflow.workflow;
                var activity = httpWorkflow.activity;
                var workflowWrapper = await workflowBlueprintReflector.ReflectAsync(serviceProvider, workflow, cancellationToken);
                var activityWrapper = workflowWrapper.GetActivity<HttpRequestReceived>(activity.Id)!;
                var path = await activityWrapper.GetPropertyValueAsync(x => x.Path, cancellationToken);
                var method = await activityWrapper.GetPropertyValueAsync(x => x.Method, cancellationToken);

                matches.Add((workflow, activity, path, method));
            }

            return matches;
        }

        private static bool AreSame(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}