using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Options;
using Elsa.Activities.Http.Parsers.Request;
using Elsa.Activities.Http.Services;
using Elsa.Models;
using Elsa.Persistence;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
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
            IOptions<HttpActivityOptions> options,
            IWorkflowLaunchpad workflowLaunchpad,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowRegistry workflowRegistry,
            IWorkflowBlueprintReflector workflowBlueprintReflector,
            IEnumerable<IHttpRequestBodyParser> contentParsers)
        {
            var basePath = options.Value.BasePath;
            var request = httpContext.Request;

            string path;
            
            // If a base path was configured, try to match against that first.
            if (basePath != null)
            {
                // If no match, continue with the next middleware in the pipeline.
                if (!request.Path.StartsWithSegments(basePath.Value, out _, out var remainingPath))
                {
                    await _next(httpContext);
                    return;
                }

                path = remainingPath.Value.ToLowerInvariant();
            }
            else
            {
                path = httpContext.Request.Path.Value.ToLowerInvariant();
            }

            var cancellationToken = CancellationToken.None; // Prevent half-way request abortion (which also happens when WriteHttpResponse writes to the response).
            var method = httpContext.Request.Method!.ToLowerInvariant();

            request.TryGetCorrelationId(out var correlationId);

            const string activityType = nameof(HttpEndpoint);
            var bookmark = new HttpEndpointBookmark(path, method);
            var collectWorkflowsContext = new WorkflowsQuery(activityType, bookmark, correlationId, default, default, TenantId);
            var pendingWorkflows = await workflowLaunchpad.FindWorkflowsAsync(collectWorkflowsContext, cancellationToken).ToList();

            if (!pendingWorkflows.Any())
            {
                // If a base path was configured, we are sure the requester tried to execute a workflow that doesn't exist.
                // Therefore, sending a 404 response seems appropriate instead of continuing with any subsequent middlewares.
                if (basePath != null)
                {
                    httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }
                
                // If no base path was configured on the other hand, the request could be targeting anything else and should be handled by subsequent middlewares. 
                await _next(httpContext);
                return;
            }

            if (pendingWorkflows.Count > 1)
            {
                httpContext.Response.ContentType = "application/json";
                httpContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;

                var responseContent = JsonConvert.SerializeObject(new
                {
                    errorMessage = "The call is ambiguous and matches multiple workflows.",
                    workflows = pendingWorkflows
                });

                await httpContext.Response.WriteAsync(responseContent, cancellationToken);
                return;
            }

            var pendingWorkflow = pendingWorkflows.Single();
            var pendingWorkflowInstance = await workflowInstanceStore.FindByIdAsync(pendingWorkflow.WorkflowInstanceId, cancellationToken);
            if (pendingWorkflowInstance is null)
            {
                await _next(httpContext);
                return;
            }

            var workflowBlueprint = await workflowRegistry.FindAsync(x => x.IsPublished && x.Id == pendingWorkflowInstance.DefinitionId, cancellationToken);
            if (workflowBlueprint is null)
            {
                await _next(httpContext);
                return;
            }

            var workflowBlueprintWrapper = await workflowBlueprintReflector.ReflectAsync(httpContext.RequestServices, workflowBlueprint, cancellationToken);
            var inputModel = new HttpRequestModel(
                request.Path.ToString(),
                request.Method,
                request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()),
                request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString())
            );

            var orderedContentParsers = contentParsers.OrderByDescending(x => x.Priority).ToList();
            var simpleContentType = request.ContentType?.Split(';').First();
            var contentParser = orderedContentParsers.FirstOrDefault(x => x.SupportedContentTypes.Contains(simpleContentType, StringComparer.OrdinalIgnoreCase)) ?? orderedContentParsers.LastOrDefault() ?? new DefaultHttpRequestBodyParser();

            var activityWrapper = workflowBlueprintWrapper.GetUnfilteredActivity<HttpEndpoint>(pendingWorkflow.ActivityId!);
            var readContent = await activityWrapper!.EvaluatePropertyValueAsync(x => x.ReadContent, cancellationToken);

            if (readContent)
            {
                var targetType = await activityWrapper.EvaluatePropertyValueAsync(x => x.TargetType, cancellationToken);
                inputModel = inputModel with { Body = await contentParser.ParseAsync(request, targetType, cancellationToken) };
            }

            var useDispatch = httpContext.Request.GetUseDispatch();
            if (useDispatch)
            {
                await workflowLaunchpad.DispatchPendingWorkflowAsync(pendingWorkflow, new WorkflowInput(inputModel), cancellationToken);

                httpContext.Response.ContentType = "application/json";
                httpContext.Response.StatusCode = (int) HttpStatusCode.Accepted;
                await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(pendingWorkflows), cancellationToken);
            }
            else
            {
                await workflowLaunchpad.ExecutePendingWorkflowAsync(pendingWorkflow, new WorkflowInput(inputModel), cancellationToken);
                pendingWorkflowInstance = await workflowInstanceStore.FindByIdAsync(pendingWorkflow.WorkflowInstanceId, cancellationToken);

                if (pendingWorkflowInstance is not null
                    && pendingWorkflowInstance.WorkflowStatus == Elsa.Models.WorkflowStatus.Faulted
                    && !httpContext.Response.HasStarted)
                {
                    httpContext.Response.ContentType = "application/json";
                    httpContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError;

                    var faultedResponse = JsonConvert.SerializeObject(new
                    {
                        errorMessage = $"Workflow faulted at {pendingWorkflowInstance.FaultedAt!} with error: {pendingWorkflowInstance.Fault!.Message}",
                        workflow = new
                        {
                            name = pendingWorkflowInstance.Name,
                            version = pendingWorkflowInstance.Version,
                            instanceId = pendingWorkflowInstance.Id
                        }
                    });

                    await httpContext.Response.WriteAsync(faultedResponse, cancellationToken);
                }
            }
        }
    }
}