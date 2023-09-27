using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Bookmarks;
using Elsa.Activities.Http.Contracts;
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
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Open.Linq.AsyncExtensions;

namespace Elsa.Activities.Http.Middleware
{
    public class HttpEndpointMiddleware
    {
        private readonly RequestDelegate _next;

        public HttpEndpointMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(
            HttpContext httpContext,
            IOptions<HttpActivityOptions> options,
            IWorkflowLaunchpad workflowLaunchpad,
            IWorkflowInstanceStore workflowInstanceStore,
            IWorkflowRegistry workflowRegistry,
            IWorkflowBlueprintReflector workflowBlueprintReflector,
            IRouteMatcher routeMatcher,
            ITenantAccessor tenantAccessor,
            IEnumerable<IHttpRequestBodyParser> contentParsers)
        {
            var basePath = options.Value.BasePath;
            var path = GetPath(basePath, httpContext);

            if (path == null)
            {
                await _next(httpContext);
                return;
            }

            var request = httpContext.Request;
            var cancellationToken = CancellationToken.None; // Prevent half-way request abortion (which also happens when WriteHttpResponse writes to the response).
            var method = httpContext.Request.Method!.ToLowerInvariant();

            var tenantId = await tenantAccessor.GetTenantIdAsync(cancellationToken);

            request.TryGetCorrelationId(out var correlationId);

            // Try to match inbound path.
            var routeTable = httpContext.RequestServices.GetRequiredService<IRouteTable>();

            var matchingRouteQuery =
                from route in routeTable
                let routeValues = routeMatcher.Match(route, path)
                where routeValues != null
                select new { route, routeValues };

            var matchingRoute = matchingRouteQuery.FirstOrDefault();
            var routeTemplate = matchingRoute?.route ?? path;
            var routeData = httpContext.GetRouteData();

            if (matchingRoute != null)
            {
                foreach (var routeValue in matchingRoute.routeValues!)
                    routeData.Values[routeValue.Key] = routeValue.Value;
            }

            // Find pending workflows using the selected route and HTTP method of the request.
            var pendingWorkflows = await FindPendingWorkflows(workflowLaunchpad, routeTemplate, method, cancellationToken, correlationId, tenantId).ToList();
            if (pendingWorkflows?.Count == 0 && string.IsNullOrWhiteSpace(matchingRoute?.route))
            {
                routeTemplate = path.StartsWith("/") ? path[1..] : $"/{path}";
                pendingWorkflows = await FindPendingWorkflows(workflowLaunchpad, routeTemplate, method, cancellationToken, correlationId, tenantId).ToList();
            }

            if (await HandleNoWorkflowsFoundAsync(httpContext, pendingWorkflows, basePath))
                return;

            if (await HandleMultipleWorkflowsFoundAsync(httpContext, pendingWorkflows, cancellationToken))
                return;

            var pendingWorkflow = pendingWorkflows.Single();
            var pendingWorkflowInstance = pendingWorkflow.WorkflowInstance ?? await workflowInstanceStore.FindByIdAsync(pendingWorkflow.WorkflowInstanceId, cancellationToken);

            if (pendingWorkflowInstance is null)
            {
                await _next(httpContext);
                return;
            }

            var isTest = pendingWorkflowInstance.GetMetadata("isTest");
            var workflowBlueprint = (isTest != null && Convert.ToBoolean(isTest))
                ? await workflowRegistry.FindAsync(pendingWorkflowInstance.DefinitionId, VersionOptions.Latest, tenantId, cancellationToken)
                : await workflowRegistry.FindAsync(pendingWorkflowInstance.DefinitionId, VersionOptions.Published, tenantId, cancellationToken);

            if (workflowBlueprint is null || workflowBlueprint.IsDisabled)
            {
                await _next(httpContext);
                return;
            }

            var workflowBlueprintWrapper = await workflowBlueprintReflector.ReflectAsync(httpContext.RequestServices, workflowBlueprint, cancellationToken);
            var orderedContentParsers = contentParsers.OrderByDescending(x => x.Priority).ToList();
            var simpleContentType = request.ContentType?.Split(';').First();
            var contentParser = orderedContentParsers.FirstOrDefault(x => x.SupportedContentTypes.Contains(simpleContentType, StringComparer.OrdinalIgnoreCase)) ?? orderedContentParsers.LastOrDefault() ?? new DefaultHttpRequestBodyParser();
            var activityWrapper = workflowBlueprintWrapper.GetUnfilteredActivity<HttpEndpoint>(pendingWorkflow.ActivityId!)!;

            if (!await AuthorizeAsync(httpContext, options.Value, activityWrapper, workflowBlueprint, pendingWorkflow, cancellationToken) ||
                !await AuthorizeWithCustomHeaderAsync(httpContext, activityWrapper, workflowBlueprint, pendingWorkflow, cancellationToken))
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return;
            }

            var readContent = await activityWrapper.EvaluatePropertyValueAsync(x => x.ReadContent, cancellationToken);

            var inputModel = new HttpRequestModel(
                request.Path.ToString(),
                request.Method,
                request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()),
                routeData.Values,
                request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString())
            );

            if (readContent)
            {
                var targetType = await activityWrapper.EvaluatePropertyValueAsync(x => x.TargetType, cancellationToken);

                async Task WriteBadRequestResponseAsync(Exception e)
                {
                    httpContext.Response.ContentType = MediaTypeNames.Application.Json;
                    httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
                    await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(new
                    {
                        error = "Could not parse content",
                        message = e.Message
                    }), cancellationToken);
                }

                try
                {
                    inputModel = inputModel with
                    {
                        RawBody = await request.ReadContentAsStringAsync(cancellationToken),
                        Body = await contentParser.ParseAsync(request, targetType, cancellationToken)
                    };
                }
                catch (JsonSerializationException e)
                {
                    await WriteBadRequestResponseAsync(e);
                    return;
                }
                catch (JsonReaderException e)
                {
                    await WriteBadRequestResponseAsync(e);
                    return;
                }
                catch (XmlException e)
                {
                    await WriteBadRequestResponseAsync(e);
                    return;
                }
            }

            var useDispatch = httpContext.Request.GetUseDispatch();

            if (useDispatch)
            {
                await workflowLaunchpad.DispatchPendingWorkflowAsync(pendingWorkflow, new WorkflowInput(inputModel), cancellationToken);

                httpContext.Response.ContentType = MediaTypeNames.Application.Json;
                httpContext.Response.StatusCode = (int)HttpStatusCode.Accepted;
                await httpContext.Response.WriteAsync(JsonConvert.SerializeObject(pendingWorkflows), cancellationToken);
            }
            else
            {
                var result = await workflowLaunchpad.ExecutePendingWorkflowAsync(pendingWorkflow, new WorkflowInput(inputModel), cancellationToken);

                pendingWorkflowInstance = await workflowInstanceStore.FindByIdAsync(pendingWorkflow.WorkflowInstanceId, cancellationToken);

                if (pendingWorkflowInstance is not null
                    && pendingWorkflowInstance.WorkflowStatus == WorkflowStatus.Faulted
                    && !httpContext.Response.HasStarted)
                {
                    var faultHandler = options.Value.HttpEndpointWorkflowFaultHandlerFactory(httpContext.RequestServices);
                    await faultHandler.HandleAsync(new HttpEndpointFaultedWorkflowContext(httpContext, pendingWorkflowInstance, result.Exception, cancellationToken));
                }
            }
        }

        private async Task<bool> AuthorizeAsync(
            HttpContext httpContext,
            HttpActivityOptions options,
            IActivityBlueprintWrapper<HttpEndpoint> httpEndpoint,
            IWorkflowBlueprint workflowBlueprint,
            CollectedWorkflow pendingWorkflow,
            CancellationToken cancellationToken)
        {
            var authorize = await httpEndpoint.EvaluatePropertyValueAsync(x => x.Authorize, cancellationToken);

            if (!authorize)
                return true;

            var authorizationHandler = options.HttpEndpointAuthorizationHandlerFactory(httpContext.RequestServices);

            return await authorizationHandler.AuthorizeAsync(new AuthorizeHttpEndpointContext(httpContext, httpEndpoint, workflowBlueprint, pendingWorkflow.WorkflowInstanceId, cancellationToken));
        }
        
        private async Task<bool> AuthorizeWithCustomHeaderAsync(
            HttpContext httpContext,
            IActivityBlueprintWrapper<HttpEndpoint> httpEndpoint,
            IWorkflowBlueprint workflowBlueprint,
            CollectedWorkflow pendingWorkflow,
            CancellationToken cancellationToken)
        {
            var authorizeWithCustomHeader = await httpEndpoint.EvaluatePropertyValueAsync(x => x.AuthorizeWithCustomHeader, cancellationToken);

            if (!authorizeWithCustomHeader)
                return true;

            var authorizationHandler = ActivatorUtilities.GetServiceOrCreateInstance<CustomHeaderAuthorizationHandler>(httpContext.RequestServices);
            return await authorizationHandler.AuthorizeAsync(new AuthorizeHttpEndpointContext(httpContext, httpEndpoint, workflowBlueprint, pendingWorkflow.WorkflowInstanceId, cancellationToken));
        }

        private async Task<bool> HandleNoWorkflowsFoundAsync(HttpContext httpContext, IList<CollectedWorkflow> pendingWorkflows, PathString? basePath)
        {
            if (pendingWorkflows.Any())
                return false;

            // If a base path was configured, we are sure the requester tried to execute a workflow that doesn't exist.
            // Therefore, sending a 404 response seems appropriate instead of continuing with any subsequent middlewares.
            if (basePath != null)
            {
                httpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return true;
            }

            // If no base path was configured on the other hand, the request could be targeting anything else and should be handled by subsequent middlewares. 
            await _next(httpContext);

            return true;
        }

        private async Task<bool> HandleMultipleWorkflowsFoundAsync(HttpContext httpContext, IList<CollectedWorkflow> pendingWorkflows, CancellationToken cancellationToken)
        {
            if (pendingWorkflows.Count <= 1)
                return false;

            httpContext.Response.ContentType = "application/json";
            httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var responseContent = JsonConvert.SerializeObject(new
            {
                errorMessage = "The call is ambiguous and matches multiple workflows.",
                workflows = pendingWorkflows
            });

            await httpContext.Response.WriteAsync(responseContent, cancellationToken);
            return true;
        }

        private string? GetPath(PathString? basePath, HttpContext httpContext) => basePath != null
            ? httpContext.Request.Path.StartsWithSegments(basePath.Value, out _, out var remainingPath) ? remainingPath.Value : null
            : httpContext.Request.Path.Value;

        private async Task<IEnumerable<CollectedWorkflow>> FindPendingWorkflows(
            IWorkflowLaunchpad workflowLaunchpad, 
            string routeTemplate, 
            string method,
            CancellationToken cancellationToken,
            string? correlationId = null, 
            string? tenantId = null)
        {
            const string activityType = nameof(HttpEndpoint);
            var bookmark = new HttpEndpointBookmark(routeTemplate, method);
            var collectWorkflowsContext = new WorkflowsQuery(activityType, bookmark, correlationId, default, default, tenantId);
            return await workflowLaunchpad.FindWorkflowsAsync(collectWorkflowsContext, cancellationToken);
        }
    }
}