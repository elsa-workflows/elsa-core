using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Http.Options;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.State;
using FastEndpoints;
using System.Diagnostics.CodeAnalysis;
using Open.Linq.AsyncExtensions;

namespace Elsa.Http.Middleware;

/// <summary>
/// An ASP.NET middleware component that tries to match the inbound request path to an associated workflow and then run that workflow.
/// </summary>
[PublicAPI]
public class WorkflowsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HttpActivityOptions _options;
    private readonly string _activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowsMiddleware(
        RequestDelegate next,
        IOptions<HttpActivityOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    /// <summary>
    /// Attempts to match the inbound request path to an associated workflow and then run that workflow.
    /// </summary>
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public async Task InvokeAsync(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        var path = GetPath(httpContext);
        var basePath = _options.BasePath?.ToString().NormalizeRoute();

        // If the request path does not match the configured base path to handle workflows, then skip.
        if (!string.IsNullOrWhiteSpace(basePath))
        {
            if (!path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                await _next(httpContext);
                return;
            }

            // Strip the base path.
            path = path[basePath.Length..];
        }

        var matchingPath = GetMatchingRoute(serviceProvider, path);
        var input = new Dictionary<string, object>
        {
            [HttpEndpoint.HttpContextInputKey] = true,
            [HttpEndpoint.RequestPathInputKey] = path
        };

        var cancellationToken = httpContext.RequestAborted;
        var request = httpContext.Request;
        var method = request.Method.ToLowerInvariant();
        var bookmarkPayload = new HttpEndpointBookmarkPayload(matchingPath, method);
        var bookmarkHasher = serviceProvider.GetRequiredService<IBookmarkHasher>();
        var bookmarkHash = bookmarkHasher.Hash(_activityTypeName, bookmarkPayload);
        var triggers = await FindTriggersAsync(serviceProvider, bookmarkHash, cancellationToken).ToList();

        if (triggers.Count > 1)
        {
            await HandleMultipleWorkflowsFoundAsync(httpContext, () => triggers.Select(x => new
            {
                x.WorkflowDefinitionId
            }), cancellationToken);
            return;
        }

        var trigger = triggers.SingleOrDefault();
        var workflowHostFactory = serviceProvider.GetRequiredService<IWorkflowHostFactory>();
        var workflowDefinitionService = serviceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var workflowInstanceManager = serviceProvider.GetRequiredService<IWorkflowInstanceManager>();
        var correlationId = await GetCorrelationIdAsync(serviceProvider, httpContext, httpContext.RequestAborted);
        var workflowInstanceId = await GetWorkflowInstanceIdAsync(serviceProvider, httpContext, httpContext.RequestAborted);
        var cancellationTokens = new CancellationTokens(cancellationToken, cancellationToken);

        if (trigger != null)
        {
            bookmarkPayload = trigger.GetPayload<HttpEndpointBookmarkPayload>();
            var workflowDefinition = await workflowDefinitionService.FindAsync(trigger.WorkflowDefinitionVersionId, cancellationToken);

            if (workflowDefinition == null)
            {
                await httpContext.Response.SendNotFoundAsync(cancellation: cancellationToken);
                return;
            }

            var workflowHost = await workflowHostFactory.CreateAsync(workflowDefinition, cancellationToken);
            if (await AuthorizeAsync(serviceProvider, httpContext, workflowHost.Workflow, bookmarkPayload, cancellationToken))
                return;

            var startParams = new StartWorkflowHostParams
            {
                Input = input,
                InstanceId = workflowInstanceId,
                CorrelationId = correlationId,
                TriggerActivityId = trigger.ActivityId,
                CancellationTokens = cancellationTokens
            };

            await ExecuteWithinTimeoutAsync(async () =>
            {
                await workflowHost.StartWorkflowAsync(startParams, cancellationToken);
                await workflowInstanceManager.SaveAsync(workflowHost.WorkflowState, cancellationToken);
            }, bookmarkPayload.RequestTimeout, httpContext);
            await HandleWorkflowFaultAsync(serviceProvider, httpContext, workflowHost.WorkflowState, cancellationToken);
            return;
        }

        var bookmarks = await FindBookmarksAsync(serviceProvider, bookmarkHash, cancellationToken).ToList();

        if (bookmarks.Count > 1)
        {
            await HandleMultipleWorkflowsFoundAsync(httpContext, () => bookmarks.Select(x => new { x.WorkflowInstanceId }), cancellationToken);
            return;
        }
        
        var bookmark = bookmarks.SingleOrDefault();

        if (bookmark != null)
        {
            bookmarkPayload = bookmark.GetPayload<HttpEndpointBookmarkPayload>();
            var workflowInstanceStore = serviceProvider.GetRequiredService<IWorkflowInstanceStore>();
            var workflowInstance = await workflowInstanceStore.FindAsync(bookmark.WorkflowInstanceId, cancellationToken);

            if (workflowInstance == null)
            {
                await httpContext.Response.SendNotFoundAsync(cancellation: cancellationToken);
                return;
            }

            var workflowDefinition = await workflowDefinitionService.FindAsync(workflowInstance.DefinitionVersionId, cancellationToken);

            if (workflowDefinition == null)
            {
                await httpContext.Response.SendNotFoundAsync(cancellation: cancellationToken);
                return;
            }

            var workflow = await workflowDefinitionService.MaterializeWorkflowAsync(workflowDefinition, cancellationToken);
            var workflowState = workflowInstance.WorkflowState;
            var workflowHost = await workflowHostFactory.CreateAsync(workflow, workflowState, cancellationToken);
            if (await AuthorizeAsync(serviceProvider, httpContext, workflowHost.Workflow, bookmarkPayload, cancellationToken))
                return;

            var resumeParams = new ResumeWorkflowHostParams
            {
                Input = input,
                CorrelationId = correlationId,
                ActivityInstanceId = bookmark.ActivityInstanceId,
                BookmarkId = bookmark.BookmarkId,
                CancellationTokens = cancellationTokens
            };

            await ExecuteWithinTimeoutAsync(async () =>
            {
                await workflowHost.ResumeWorkflowAsync(resumeParams, cancellationToken);
                await workflowInstanceManager.SaveAsync(workflowHost.WorkflowState, cancellationToken);
            }, bookmarkPayload.RequestTimeout, httpContext);
            await HandleWorkflowFaultAsync(serviceProvider, httpContext, workflowHost.WorkflowState, cancellationToken);
            return;
        }

        // If a base path was configured, the requester tried to execute a workflow that doesn't exist.
        if (basePath != null)
        {
            await httpContext.Response.SendNotFoundAsync(cancellation: cancellationToken);
            return;
        }

        // If no base path was configured, the request should be handled by subsequent middlewares. 
        await _next(httpContext);
    }

    private async Task<IEnumerable<StoredTrigger>> FindTriggersAsync(IServiceProvider serviceProvider, string bookmarkHash, CancellationToken cancellationToken)
    {
        var triggerStore = serviceProvider.GetRequiredService<ITriggerStore>();
        var triggerFilter = new TriggerFilter
        {
            Hash = bookmarkHash
        };
        return await triggerStore.FindManyAsync(triggerFilter, cancellationToken);
    }

    private async Task<IEnumerable<StoredBookmark>> FindBookmarksAsync(IServiceProvider serviceProvider, string bookmarkHash, CancellationToken cancellationToken)
    {
        var bookmarkStore = serviceProvider.GetRequiredService<IBookmarkStore>();
        var bookmarkFilter = new BookmarkFilter
        {
            Hash = bookmarkHash
        };
        return await bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken);
    }

    private async Task ExecuteWithinTimeoutAsync(Func<Task> action, TimeSpan? requestTimeout, HttpContext httpContext)
    {
        if (requestTimeout == null)
        {
            await action();
            return;
        }

        using var cts = new CancellationTokenSource(requestTimeout.Value);
        var originalCancellationToken = httpContext.RequestAborted;
        httpContext.RequestAborted = cts.Token;
        await action();
        httpContext.RequestAborted = originalCancellationToken;
    }

    private string GetMatchingRoute(IServiceProvider serviceProvider, string path)
    {
        var routeMatcher = serviceProvider.GetRequiredService<IRouteMatcher>();
        var routeTable = serviceProvider.GetRequiredService<IRouteTable>();

        var matchingRouteQuery =
            from route in routeTable
            let routeValues = routeMatcher.Match(route, path)
            where routeValues != null
            select new
            {
                route,
                routeValues
            };

        var matchingRoute = matchingRouteQuery.FirstOrDefault();
        var routeTemplate = matchingRoute?.route ?? path;

        return routeTemplate;
    }

    private async Task<string?> GetCorrelationIdAsync(IServiceProvider serviceProvider, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var correlationIdSelectors = serviceProvider.GetServices<IHttpCorrelationIdSelector>();

        var correlationId = default(string);

        foreach (var selector in correlationIdSelectors.OrderByDescending(x => x.Priority))
        {
            correlationId = await selector.GetCorrelationIdAsync(httpContext, cancellationToken);

            if (correlationId != null)
                break;
        }

        return correlationId;
    }

    private async Task<string?> GetWorkflowInstanceIdAsync(IServiceProvider serviceProvider, HttpContext httpContext, CancellationToken cancellationToken)
    {
        var workflowInstanceIdSelectors = serviceProvider.GetServices<IHttpWorkflowInstanceIdSelector>();

        var workflowInstanceId = default(string);

        foreach (var selector in workflowInstanceIdSelectors.OrderByDescending(x => x.Priority))
        {
            workflowInstanceId = await selector.GetWorkflowInstanceIdAsync(httpContext, cancellationToken);

            if (workflowInstanceId != null)
                break;
        }

        return workflowInstanceId;
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

    private string GetPath(HttpContext httpContext) => httpContext.Request.Path.Value!.NormalizeRoute();

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private async Task<bool> HandleMultipleWorkflowsFoundAsync(HttpContext httpContext, Func<IEnumerable<object>> workflowMatches, CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var responseContent = JsonSerializer.Serialize(new
        {
            errorMessage = "The call is ambiguous and matches multiple workflows.",
            workflows = workflowMatches().ToArray()
        });

        await httpContext.Response.WriteAsync(responseContent, cancellationToken);
        return true;
    }

    private async Task<bool> HandleWorkflowFaultAsync(IServiceProvider serviceProvider, HttpContext httpContext, WorkflowState workflowState, CancellationToken cancellationToken)
    {
        if (!workflowState.Incidents.Any() || httpContext.Response.HasStarted)
            return false;

        var httpEndpointFaultHandler = serviceProvider.GetRequiredService<IHttpEndpointFaultHandler>();
        await httpEndpointFaultHandler.HandleAsync(new HttpEndpointFaultContext(httpContext, workflowState, cancellationToken));
        return true;
    }

    private async Task<bool> AuthorizeAsync(
        IServiceProvider serviceProvider,
        HttpContext httpContext,
        Workflow workflow,
        HttpEndpointBookmarkPayload bookmarkPayload,
        CancellationToken cancellationToken)
    {
        var httpEndpointAuthorizationHandler = serviceProvider.GetRequiredService<IHttpEndpointAuthorizationHandler>();

        if (!(bookmarkPayload.Authorize ?? false))
            return false;

        var authorized = await httpEndpointAuthorizationHandler.AuthorizeAsync(new AuthorizeHttpEndpointContext(httpContext, workflow, bookmarkPayload.Policy));

        if (!authorized)
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

        return !authorized;
    }
}