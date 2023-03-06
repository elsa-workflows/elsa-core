using Elsa.Common.Models;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Http.Models;
using Elsa.Http.Options;
using Elsa.Http.Services;
using Elsa.Workflows.Core.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Runtime.Services;
using JetBrains.Annotations;
using Jint;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mime;
using System.Text.Json;

namespace Elsa.Http.Middleware;

/// <summary>
/// An ASP.NET middleware component that tries to match the inbound request path to an associated workflow and then run that workflow.
/// </summary>
[PublicAPI]
public class WorkflowsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IWorkflowRuntime _workflowRuntime;
    private readonly IWorkflowHostFactory _workflowHostFactory;
    private readonly IWorkflowDefinitionService _workflowDefinitionService;
    private readonly IHttpBookmarkProcessor _httpBookmarkProcessor;
    private readonly IWorkflowInstanceStore _workflowInstanceStore;
    private readonly IHttpEndpointWorkflowFaultHandler _httpEndpointWorkflowFaultHandler;
    private readonly IHttpEndpointAuthorizationHandler _httpEndpointAuthorizationHandler;
    private readonly IWorkflowExecutionContextFactory _workflowExecutionContextFactory;
    private readonly IBookmarkStore _bookmarkStore;
    private readonly IBookmarkHasher _hasher;
    private readonly IBookmarkPayloadSerializer _serializer;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly HttpActivityOptions _options;
    private readonly string _activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowsMiddleware(
        RequestDelegate next,
        IWorkflowRuntime workflowRuntime,
        IWorkflowHostFactory workflowHostFactory,
        IWorkflowDefinitionService workflowDefinitionService,
        IHttpBookmarkProcessor httpBookmarkProcessor,
        IWorkflowInstanceStore workflowInstanceStore,
        IOptions<HttpActivityOptions> options,
        IHttpEndpointWorkflowFaultHandler httpEndpointWorkflowFaultHandler,
        IHttpEndpointAuthorizationHandler httpEndpointAuthorizationHandler,
        IWorkflowExecutionContextFactory workflowExecutionContextFactory,
        IBookmarkStore bookmarkStore,
        IBookmarkHasher hasher,
        IBookmarkPayloadSerializer serializer,
        IServiceScopeFactory serviceScopeFactory)
    {
        _next = next;
        _workflowRuntime = workflowRuntime;
        _workflowHostFactory = workflowHostFactory;
        _workflowDefinitionService = workflowDefinitionService;
        _httpBookmarkProcessor = httpBookmarkProcessor;
        _workflowInstanceStore = workflowInstanceStore;
        _httpEndpointWorkflowFaultHandler = httpEndpointWorkflowFaultHandler;
        _httpEndpointAuthorizationHandler = httpEndpointAuthorizationHandler;
        _workflowExecutionContextFactory = workflowExecutionContextFactory;
        _bookmarkStore = bookmarkStore;
        _hasher = hasher;
        _serializer = serializer;
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
    }


    /// <summary>
    /// Attempts to matches the inbound request path to an associated workflow and then run that workflow.
    /// </summary>
    public async Task InvokeAsync(HttpContext httpContext, IRouteMatcher routeMatcher)
    {
        var path = GetPath(httpContext);
        var basePath = _options.BasePath;

        // If the request path does not match the configured base path to handle workflows, then skip.
        if (!string.IsNullOrWhiteSpace(basePath))
        {
            if (!path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                await _next(httpContext);
                return;
            }

            // Strip the base path.
            path = path.Substring(basePath.Value.Value.Length);
        }

        var input = new Dictionary<string, object>
        {
            [HttpEndpoint.HttpContextInputKey] = true,
            [HttpEndpoint.RequestPathInputKey] = path
        };

        // TODO: Get correlation ID from query string or header etc.
        var correlationId = default(string);
        var request = httpContext.Request;
        var method = request.Method!.ToLowerInvariant();
        var bookmarkPayload = new HttpEndpointBookmarkPayload(path, method);
        var triggerOptions = new TriggerWorkflowsRuntimeOptions(correlationId, input);
        var cancellationToken = httpContext.RequestAborted;

        var hash = _hasher.Hash(_activityTypeName, bookmarkPayload);
        var bookmarkFilter = new BookmarkFilter() { };
        var x = (await _bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken));//.Select(x => _serializer.Deserialize<HttpEndpointBookmarkPayload>(x.Data!)).ToList();

        // Trigger the workflow.
        var triggerResult = await _workflowRuntime.TriggerWorkflowsAsync(_activityTypeName, bookmarkPayload, triggerOptions, cancellationToken);

        if (await HandleNoWorkflowsFoundAsync(httpContext, triggerResult.TriggeredWorkflows, basePath))
            return;

        if (await HandleMultipleWorkflowsFoundAsync(httpContext, triggerResult.TriggeredWorkflows, cancellationToken))
            return;

        if (await HandleWorkflowFaultAsync(httpContext, triggerResult, cancellationToken))
            return;

        var workflowState = await _workflowRuntime.ExportWorkflowStateAsync(triggerResult.TriggeredWorkflows.Single().InstanceId, cancellationToken);
        var workflowDefinition = await _workflowDefinitionService.FindAsync(workflowState.DefinitionId, VersionOptions.SpecificVersion(workflowState.DefinitionVersion), cancellationToken);

        if (workflowDefinition == null)
            throw new Exception("Workflow definition not found");

        var workflow = await _workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
        var workflowExecutionContext = await _workflowExecutionContextFactory.CreateAsync(_serviceScopeFactory.CreateScope().ServiceProvider, workflow, triggerResult.TriggeredWorkflows.Single().InstanceId, workflowState, cancellationToken: cancellationToken);


        var activity = workflowExecutionContext.FindActivityByActivityId(triggerResult.TriggeredWorkflows.Single().ActivityId);
        var activityExecutionContext = workflowExecutionContext.CreateActivityExecutionContext(activity);

        var xx = activity as HttpEndpoint;
        if (await AuthorizeAsync(activityExecutionContext, httpContext, xx, triggerResult))
            return;

        // Process the trigger result by resuming each HTTP bookmark, if any.
        await _httpBookmarkProcessor.ProcessBookmarks(triggerResult.TriggeredWorkflows, correlationId, input, cancellationToken);
    }

    private static async Task WriteResponseAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        var response = httpContext.Response;

        if (!response.HasStarted)
        {
            response.ContentType = MediaTypeNames.Application.Json;
            response.StatusCode = StatusCodes.Status200OK;

            var model = new
            {
                workflowInstanceIds = Array.Empty<string>(),
            };

            var json = JsonSerializer.Serialize(model);
            await response.WriteAsync(json, cancellationToken);
        }
    }

    private string GetPath(HttpContext httpContext) => httpContext.Request.Path.Value.ToLowerInvariant();

    private async Task<bool> HandleNoWorkflowsFoundAsync(HttpContext httpContext, ICollection<WorkflowExecutionResult> triggeredWorkflows, PathString? basePath)
    {
        if (triggeredWorkflows.Any())
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

    private async Task<bool> HandleMultipleWorkflowsFoundAsync(HttpContext httpContext, ICollection<WorkflowExecutionResult> triggeredWorkflows, CancellationToken cancellationToken)
    {
        if (triggeredWorkflows.Count <= 1)
            return false;

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var responseContent = JsonSerializer.Serialize(new
        {
            errorMessage = "The call is ambiguous and matches multiple workflows.",
            workflows = triggeredWorkflows
        });

        await httpContext.Response.WriteAsync(responseContent, cancellationToken);
        return true;
    }

    private async Task<bool> HandleWorkflowFaultAsync(HttpContext httpContext, TriggerWorkflowsResult triggerResult, CancellationToken cancellationToken)
    {
        var instanceFilter = new WorkflowInstanceFilter { Id = triggerResult.TriggeredWorkflows.Single().InstanceId };
        var workflowInstance = await _workflowInstanceStore.FindAsync(instanceFilter, cancellationToken);

        if (workflowInstance is not null
            && workflowInstance.SubStatus == WorkflowSubStatus.Faulted
            && !httpContext.Response.HasStarted)
        {
            await _httpEndpointWorkflowFaultHandler.HandleAsync(new HttpEndpointFaultedWorkflowContext(httpContext, workflowInstance, null, cancellationToken));
            return true;
        }

        return false;
    }

    private async Task<bool> AuthorizeAsync(
        ActivityExecutionContext expressionExecutionContext,
        HttpContext httpContext,
        HttpEndpoint httpEndpoint,
        TriggerWorkflowsResult triggerResult)
    {
        var authorize = httpEndpoint.Authorize.TryGet(expressionExecutionContext);
        /*
        if (!authorize)
            return true;

        var workflowInstanceId = triggerResult.TriggeredWorkflows.Single().InstanceId;

        var authorized = await _httpEndpointAuthorizationHandler.AuthorizeAsync(new AuthorizeHttpEndpointContext(expressionExecutionContext, httpContext, httpEndpoint, workflowInstanceId));

        if (!authorized)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
        */
        return true;//!authorized;
    }
}