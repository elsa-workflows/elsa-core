using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Persistence;
using Elsa.Persistence.Specifications.WorkflowInstances;
using Elsa.Services;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Http.Middleware
{
    public class HttpEndpointMiddleware
    {
        // TODO: Design multi-tenancy. 
        private const string? TenantId = default;
        private readonly RequestDelegate _next;

        public HttpEndpointMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(
            HttpContext httpContext,
            IWorkflowLaunchpad workflowLaunchpad,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowRegistry workflowRegistry,
            IWorkflowBlueprintReflector workflowBlueprintReflector,
            IEnumerable<IHttpRequestBodyParser> contentParsers)
        {
            var path = httpContext.Request.Path.Value.ToLowerInvariant();
            var method = httpContext.Request.Method!.ToLowerInvariant();
            var cancellationToken = httpContext.RequestAborted;
            var request = httpContext.Request;
            request.TryGetCorrelationId(out var correlationId);
            var useDispatch = httpContext.Request.GetUseDispatch();

            const string activityType = nameof(HttpEndpoint);
            var trigger = new HttpEndpointBookmark(path, method, null);
            var bookmark = new HttpEndpointBookmark(path, method, correlationId?.ToLowerInvariant());
            var collectWorkflowsContext = new CollectWorkflowsContext(activityType, bookmark, trigger, default, correlationId, default, TenantId);
            var pendingWorkflows = await workflowLaunchpad.CollectWorkflowsAsync(collectWorkflowsContext, cancellationToken).ToList();
            var pendingWorkflowInstanceIds = pendingWorkflows.Select(x => x.WorkflowInstanceId).Distinct();
            var pendingWorkflowInstances = (await workflowInstanceStore.FindManyAsync(new WorkflowInstanceIdsSpecification(pendingWorkflowInstanceIds), cancellationToken: cancellationToken)).ToDictionary(x => x.Id);
            var workflowDefinitionIds = pendingWorkflowInstances.Values.Select(x => x.DefinitionId).Distinct().ToHashSet();
            var workflowBlueprints = (await workflowRegistry.FindManyAsync(x => workflowDefinitionIds.Contains(x.Id), cancellationToken)).ToDictionary(x => x.Id);
            var serviceProvider = httpContext.RequestServices;
            var workflowBlueprintWrappers = (await Task.WhenAll(workflowBlueprints.Values.Select(async x => await workflowBlueprintReflector.ReflectAsync(serviceProvider, x, cancellationToken)))).ToDictionary(x => x.WorkflowBlueprint.Id);

            var commonInputModel = new HttpRequestModel(
                new Uri(request.Path.ToString(), UriKind.Relative),
                request.Method,
                request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()),
                request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString())
            );

            var orderedContentParsers = contentParsers.OrderByDescending(x => x.Priority).ToList();
            var simpleContentType = request.ContentType?.Split(';').First();
            var contentParser = orderedContentParsers.FirstOrDefault(x => x.SupportedContentTypes.Contains(simpleContentType, StringComparer.OrdinalIgnoreCase)) ?? orderedContentParsers.Last();

            foreach (var pendingWorkflow in pendingWorkflows)
            {
                var pendingWorkflowInstance = pendingWorkflowInstances[pendingWorkflow.WorkflowInstanceId];
                var workflowBlueprintWrapper = workflowBlueprintWrappers[pendingWorkflowInstance.DefinitionId];
                var activityWrapper = workflowBlueprintWrapper.GetActivity<HttpEndpoint>(pendingWorkflow.ActivityId!);
                var readContent = await activityWrapper!.GetPropertyValueAsync(x => x.ReadContent, cancellationToken);
                var inputModel = commonInputModel;

                if (readContent)
                {
                    var targetType = await activityWrapper.GetPropertyValueAsync(x => x.TargetType, cancellationToken);
                    inputModel = inputModel with { Body = await contentParser.ParseAsync(request, targetType, cancellationToken) };
                }

                if (useDispatch)
                    await workflowLaunchpad.DispatchPendingWorkflowAsync(pendingWorkflow, inputModel, cancellationToken);
                else
                    await workflowLaunchpad.ExecutePendingWorkflowsAsync(pendingWorkflows, commonInputModel, cancellationToken);
            }

            if (pendingWorkflows.Count > 0)
            {
                if (useDispatch)
                {
                    httpContext.Response.ContentType = "application/json";
                    await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(pendingWorkflows), cancellationToken);
                }

                return;
            }

            await _next(httpContext);
        }
    }
}