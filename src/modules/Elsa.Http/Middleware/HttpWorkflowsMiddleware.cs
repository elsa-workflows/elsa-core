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
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Entities;
using Elsa.Workflows.Runtime.Parameters;
using Elsa.Workflows.State;
using FastEndpoints;
using System.Diagnostics.CodeAnalysis;
using Elsa.Workflows.Contracts;
using Microsoft.Extensions.Logging;
using Open.Linq.AsyncExtensions;

namespace Elsa.Http.Middleware;

/// <summary>
/// An ASP.NET middleware component that tries to match the inbound request path to an associated workflow and then run that workflow.
/// </summary>
[PublicAPI]
public class HttpWorkflowsMiddleware(RequestDelegate next, IOptions<HttpActivityOptions> options, ILogger<HttpWorkflowsMiddleware> logger)
{
    private readonly string _activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();

    /// <summary>
    /// Attempts to match the inbound request path to an associated workflow and then run that workflow.
    /// </summary>
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public async Task InvokeAsync(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        var path = GetPath(httpContext);
        var basePath = options.Value.BasePath?.ToString().NormalizeRoute();

        // If the request path does not match the configured base path to handle workflows, then skip.
        if (!string.IsNullOrWhiteSpace(basePath))
        {
            if (!path.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                await next(httpContext);
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
        var httpWorkflowLookupService = serviceProvider.GetRequiredService<IHttpWorkflowLookupService>();
        var bookmarkHash = ComputeBookmarkHash(serviceProvider, matchingPath, method);
        var lookupResult = await httpWorkflowLookupService.FindWorkflowAsync(bookmarkHash, cancellationToken);

        if (lookupResult != null)
        {
            var triggers = lookupResult.Triggers;

            if (triggers?.Count > 1)
            {
                await HandleMultipleWorkflowsFoundAsync(httpContext, () => triggers.Select(x => new
                {
                    x.WorkflowDefinitionId
                }), cancellationToken);
                return;
            }

            var trigger = triggers?.FirstOrDefault();
            if (trigger != null)
            {
                var workflowGraph = lookupResult.WorkflowGraph!;
                await StartWorkflowAsync(httpContext, trigger, workflowGraph, input);
                return;
            }
        }

        var bookmarks = await FindBookmarksAsync(serviceProvider, bookmarkHash, cancellationToken).ToList();

        if (bookmarks.Count > 1)
        {
            await HandleMultipleWorkflowsFoundAsync(httpContext, () => bookmarks.Select(x => new
            {
                x.WorkflowInstanceId
            }), cancellationToken);
            return;
        }

        var bookmark = bookmarks.SingleOrDefault();

        if (bookmark != null)
        {
            await ResumeWorkflowAsync(httpContext, bookmark, input);
            return;
        }

        // If a base path was configured, the requester tried to execute a workflow that doesn't exist.
        if (basePath != null)
        {
            await httpContext.Response.SendNotFoundAsync(cancellation: cancellationToken);
            return;
        }

        // If no base path was configured, the request should be handled by subsequent middlewares. 
        await next(httpContext);
    }

    private async Task<WorkflowGraph?> FindWorkflowGraphAsync(IServiceProvider serviceProvider, StoredTrigger trigger, CancellationToken cancellationToken)
    {
        var workflowDefinitionService = serviceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var workflowDefinitionId = trigger.WorkflowDefinitionVersionId;
        return await workflowDefinitionService.FindWorkflowGraphAsync(workflowDefinitionId, cancellationToken);
    }

    private async Task StartWorkflowAsync(HttpContext httpContext, StoredTrigger trigger, WorkflowGraph workflowGraph, IDictionary<string, object> input)
    {
        var serviceProvider = httpContext.RequestServices;
        var cancellationToken = httpContext.RequestAborted;
        var bookmarkPayload = trigger.GetPayload<HttpEndpointBookmarkPayload>();
        var workflowHostFactory = serviceProvider.GetRequiredService<IWorkflowHostFactory>();
        var workflowHost = await workflowHostFactory.CreateAsync(workflowGraph, cancellationToken);
        if (await AuthorizeAsync(serviceProvider, httpContext, workflowHost.Workflow, bookmarkPayload, cancellationToken))
            return;

        await ExecuteWithinTimeoutAsync(async ct =>
        {
            var cancellationTokens = new CancellationTokens(ct, ct);
            var workflowInstanceId = await GetWorkflowInstanceIdAsync(serviceProvider, httpContext, httpContext.RequestAborted);
            var correlationId = await GetCorrelationIdAsync(serviceProvider, httpContext, httpContext.RequestAborted);
            var startParams = new StartWorkflowHostParams
            {
                Input = input,
                InstanceId = workflowInstanceId,
                CorrelationId = correlationId,
                TriggerActivityId = trigger.ActivityId,
                CancellationTokens = cancellationTokens
            };
            await workflowHost.StartWorkflowAsync(startParams, ct);
            await workflowHost.PersistStateAsync(ct);
        }, bookmarkPayload.RequestTimeout, httpContext);
        await HandleWorkflowFaultAsync(serviceProvider, httpContext, workflowHost.WorkflowState, cancellationToken);
    }

    private async Task ResumeWorkflowAsync(HttpContext httpContext, StoredBookmark bookmark, IDictionary<string, object> input)
    {
        var serviceProvider = httpContext.RequestServices;
        var cancellationToken = httpContext.RequestAborted;
        var bookmarkPayload = bookmark.GetPayload<HttpEndpointBookmarkPayload>();
        var workflowInstanceStore = serviceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var workflowInstance = await workflowInstanceStore.FindAsync(bookmark.WorkflowInstanceId, cancellationToken);

        if (workflowInstance == null)
        {
            logger.LogWarning("Bookmark {BookmarkId} references workflow instance {WorkflowInstanceId}, but no such workflow instance was found", bookmark.BookmarkId, bookmark.WorkflowInstanceId);
            await httpContext.Response.SendNotFoundAsync(cancellation: cancellationToken);
            return;
        }

        var workflowDefinitionService = serviceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var workflow = await workflowDefinitionService.FindWorkflowGraphAsync(workflowInstance.DefinitionVersionId, cancellationToken);

        if (workflow == null)
        {
            logger.LogWarning("Workflow instance {WorkflowInstanceId} references workflow definition version {WorkflowDefinitionVersionId}, but no such workflow definition version was found", workflowInstance.DefinitionVersionId, workflowInstance.DefinitionVersionId);
            await httpContext.Response.SendNotFoundAsync(cancellation: cancellationToken);
            return;
        }

        var workflowState = workflowInstance.WorkflowState;
        var workflowHostFactory = serviceProvider.GetRequiredService<IWorkflowHostFactory>();
        var workflowHost = await workflowHostFactory.CreateAsync(workflow, workflowState, cancellationToken);
        if (await AuthorizeAsync(serviceProvider, httpContext, workflowHost.Workflow, bookmarkPayload, cancellationToken))
            return;

        await ExecuteWithinTimeoutAsync(async ct =>
        {
            var correlationId = await GetCorrelationIdAsync(serviceProvider, httpContext, ct);
            var cancellationTokens = new CancellationTokens(ct, ct);
            var resumeParams = new ResumeWorkflowHostParams
            {
                Input = input,
                CorrelationId = correlationId,
                ActivityInstanceId = bookmark.ActivityInstanceId,
                BookmarkId = bookmark.BookmarkId,
                CancellationTokens = cancellationTokens
            };
            await workflowHost.ResumeWorkflowAsync(resumeParams, ct);
            await workflowHost.PersistStateAsync(ct);
        }, bookmarkPayload.RequestTimeout, httpContext);
        await HandleWorkflowFaultAsync(serviceProvider, httpContext, workflowHost.WorkflowState, cancellationToken);
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

    private async Task ExecuteWithinTimeoutAsync(Func<CancellationToken, Task> action, TimeSpan? requestTimeout, HttpContext httpContext)
    {
        // If no request timeout is specified, execute the action without any timeout.
        if (requestTimeout == null)
        {
            await action(httpContext.RequestAborted);
            return;
        }

        // Create a combined cancellation token that cancels when the request is aborted or when the request timeout is reached.
        using var requestTimeoutCancellationTokenSource = new CancellationTokenSource();
        requestTimeoutCancellationTokenSource.CancelAfter(requestTimeout.Value);
        using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted, requestTimeoutCancellationTokenSource.Token);
        var originalCancellationToken = httpContext.RequestAborted;

        // Replace the original cancellation token with the combined one.
        httpContext.RequestAborted = combinedTokenSource.Token;

        // Execute the action.
        await action(httpContext.RequestAborted);

        // Restore the original cancellation token.
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

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
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

    private string ComputeBookmarkHash(IServiceProvider serviceProvider, string path, string method)
    {
        var bookmarkPayload = new HttpEndpointBookmarkPayload(path, method);
        var bookmarkHasher = serviceProvider.GetRequiredService<IBookmarkHasher>();
        var activityTypeName = ActivityTypeNameHelper.GenerateTypeName<HttpEndpoint>();
        return bookmarkHasher.Hash(activityTypeName, bookmarkPayload);
    }
}