using Elsa.Common.DistributedHosting;
using Elsa.Extensions;
using Elsa.Http.Bookmarks;
using Elsa.Http.Contracts;
using Elsa.Http.Options;
using Elsa.Workflows.Runtime.Filters;
using JetBrains.Annotations;
using Medallion.Threading;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Runtime.Entities;
using FastEndpoints;
using System.Diagnostics.CodeAnalysis;
using Elsa.Workflows;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Management.Exceptions;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime;
using Open.Linq.AsyncExtensions;

namespace Elsa.Http.Middleware;

/// <summary>
/// An ASP.NET middleware component that tries to match the inbound request path to an associated workflow and then run that workflow.
/// </summary>
[PublicAPI]
public class HttpWorkflowsMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Attempts to match the inbound request path to an associated workflow and then run that workflow.
    /// </summary>
    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    public async Task InvokeAsync(
        HttpContext httpContext,
        IServiceProvider serviceProvider,
        IOptions<HttpActivityOptions> options,
        IHttpWorkflowLookupService httpWorkflowLookupService)
    {
        var path = httpContext.Request.Path.Value!.NormalizeRoute();
        var basePath = options.Value.BasePath?.ToString().NormalizeRoute();

        if (!string.IsNullOrWhiteSpace(basePath) && !IsBasePathMatch(path, basePath))
        {
            await next(httpContext);
            return;
        }

        var matchingPath = GetMatchingRoute(serviceProvider, path).Route;
        matchingPath = TryStripBasePath(matchingPath, basePath) ?? matchingPath;

        // Default hosts keep the original pre-lookup 503 while paused or draining. Combined hosts
        // register IHttpWorkflowUnmatchedHandler, which opts into lookup-then-reject so unmatched
        // Studio pages can still be served without executing matched HTTP work.
        var unmatchedHandler = serviceProvider.GetService<IHttpWorkflowUnmatchedHandler>();
        if (unmatchedHandler is null && TryRejectWhenNotAcceptingWork(httpContext, serviceProvider))
        {
            return;
        }

        matchingPath = matchingPath.NormalizeRoute();
        
        var input = new Dictionary<string, object>
        {
            [HttpEndpoint.HttpContextInputKey] = true,
            [HttpEndpoint.PathInputKey] = path
        };

        var cancellationToken = httpContext.RequestAborted;
        var request = httpContext.Request;
        var method = request.Method.ToLowerInvariant();
        var workflowInstanceId = await GetWorkflowInstanceIdAsync(serviceProvider, httpContext, cancellationToken);
        var correlationId = await GetCorrelationIdAsync(serviceProvider, httpContext, cancellationToken);
        var bookmarkHash = ComputeBookmarkHash(serviceProvider, matchingPath, method);
        var lookupResult = await httpWorkflowLookupService.FindWorkflowAsync(bookmarkHash, cancellationToken);

        if (lookupResult != null)
        {
            var triggers = lookupResult.Triggers;

            if (triggers.Count > 1)
            {
                if (TryRejectWhenNotAcceptingWork(httpContext, serviceProvider))
                {
                    return;
                }

                await HandleMultipleWorkflowsFoundAsync(httpContext, cancellationToken);
                return;
            }

            var trigger = triggers.FirstOrDefault();
            if (trigger != null)
            {
                if (TryRejectWhenNotAcceptingWork(httpContext, serviceProvider))
                {
                    return;
                }

                var workflowGraph = lookupResult.WorkflowGraph!;
                await StartWorkflowAsync(httpContext, trigger, workflowGraph, input, workflowInstanceId, correlationId);
                return;
            }
        }

        var bookmarks = await FindBookmarksAsync(serviceProvider, bookmarkHash, workflowInstanceId, correlationId, cancellationToken).ToList();

        if (bookmarks.Count > 1)
        {
            if (TryRejectWhenNotAcceptingWork(httpContext, serviceProvider))
            {
                return;
            }

            await HandleMultipleWorkflowsFoundAsync(httpContext, cancellationToken);
            return;
        }

        var bookmark = bookmarks.SingleOrDefault();

        if (bookmark != null)
        {
            if (TryRejectWhenNotAcceptingWork(httpContext, serviceProvider))
            {
                return;
            }

            await ResumeWorkflowAsync(httpContext, bookmark, input, correlationId);
            return;
        }

        if (unmatchedHandler is not null && await unmatchedHandler.HandleAsync(httpContext, next, cancellationToken))
        {
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

    private async Task<IEnumerable<StoredTrigger>> FindTriggersAsync(IServiceProvider serviceProvider, string bookmarkHash, CancellationToken cancellationToken)
    {
        var triggerStore = serviceProvider.GetRequiredService<ITriggerStore>();
        var triggerFilter = new TriggerFilter
        {
            Hash = bookmarkHash
        };
        return await triggerStore.FindManyAsync(triggerFilter, cancellationToken);
    }

    private async Task<IEnumerable<StoredBookmark>> FindBookmarksAsync(IServiceProvider serviceProvider, string bookmarkHash, string? workflowInstanceId, string? correlationId, CancellationToken cancellationToken)
    {
        var bookmarkStore = serviceProvider.GetRequiredService<IBookmarkStore>();
        var bookmarkFilter = new BookmarkFilter
        {
            Hash = bookmarkHash,
            WorkflowInstanceId = workflowInstanceId,
            CorrelationId = correlationId
        };
        return await bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken);
    }

    private async Task StartWorkflowAsync(HttpContext httpContext, StoredTrigger trigger, WorkflowGraph workflowGraph, IDictionary<string, object> input, string? workflowInstanceId, string? correlationId)
    {
        var bookmarkPayload = trigger.GetPayload<HttpEndpointBookmarkPayload>();
        if (!await TryAuthorizeAsync(httpContext, workflowGraph.Workflow, bookmarkPayload))
            return;

        var workflowOptions = new RunWorkflowOptions
        {
            Input = input,
            CorrelationId = correlationId,
            TriggerActivityId = trigger.ActivityId,
            WorkflowInstanceId = workflowInstanceId,
            Properties = TriggerWorkflowProperties.WithStimulusHash(null, trigger.Hash)
        };

        await ExecuteWorkflowAsync(httpContext, workflowGraph, workflowOptions, bookmarkPayload, null, input);
    }

    private async Task ResumeWorkflowAsync(HttpContext httpContext, StoredBookmark bookmark, IDictionary<string, object> input, string? correlationId)
    {
        var serviceProvider = httpContext.RequestServices;
        var cancellationToken = httpContext.RequestAborted;
        var bookmarkPayload = bookmark.GetPayload<HttpEndpointBookmarkPayload>();
        var workflowInstanceStore = serviceProvider.GetRequiredService<IWorkflowInstanceStore>();
        var workflowInstance = await workflowInstanceStore.FindAsync(bookmark.WorkflowInstanceId, cancellationToken);

        if (workflowInstance == null || workflowInstance.Status == WorkflowStatus.Finished)
        {
            await httpContext.Response.SendNotFoundAsync(cancellation: cancellationToken);
            return;
        }

        var definitionVersionId = workflowInstance.DefinitionVersionId;
        var workflowDefinitionService = serviceProvider.GetRequiredService<IWorkflowDefinitionService>();
        var workflowGraph = await workflowDefinitionService.FindWorkflowGraphAsync(definitionVersionId, cancellationToken);

        if (workflowGraph == null)
        {
            await httpContext.Response.SendNotFoundAsync(cancellation: cancellationToken);
            return;
        }

        // Require endpoint authorization before the request can contend for the instance lock.
        if (!await TryAuthorizeAsync(httpContext, workflowGraph.Workflow, bookmarkPayload))
            return;

        // Take the same per-instance lock the workflow runtime uses (see DistributedWorkflowClient), so that
        // loading, checking and running the instance can't interleave with another resume of the same instance.
        // The lock is held until the run completes, so nothing in the run may resume this same instance inline
        // (for example through IWorkflowClient): it would wait on this lock until its own timeout.
        // Don't wait for the lock longer than the request is allowed to take (only a positive timeout caps the wait).
        var lockProvider = serviceProvider.GetRequiredService<IDistributedLockProvider>();
        var lockTimeout = serviceProvider.GetRequiredService<IOptions<DistributedLockingOptions>>().Value.LockAcquisitionTimeout;
        if (bookmarkPayload.RequestTimeout is { } requestTimeout
            && requestTimeout > TimeSpan.Zero
            && (lockTimeout == Timeout.InfiniteTimeSpan || requestTimeout < lockTimeout))
            lockTimeout = requestTimeout;

        await using var instanceLock = await lockProvider.TryAcquireLockAsync($"workflow-instance:{bookmark.WorkflowInstanceId}", lockTimeout, cancellationToken);

        // The instance is busy: another run holds the lock. The caller can try again later.
        if (instanceLock == null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
            return;
        }

        workflowInstance = await workflowInstanceStore.FindAsync(bookmark.WorkflowInstanceId, cancellationToken);

        // Resume only the authorized workflow version and recheck the bookmark against current state.
        if (workflowInstance == null
            || workflowInstance.Status == WorkflowStatus.Finished
            || workflowInstance.DefinitionVersionId != definitionVersionId
            || workflowInstance.WorkflowState.Bookmarks.All(x => x.Id != bookmark.Id))
        {
            await httpContext.Response.SendNotFoundAsync(cancellation: cancellationToken);
            return;
        }

        var runWorkflowParams = new RunWorkflowOptions
        {
            WorkflowInstanceId = workflowInstance.Id,
            Input = input,
            CorrelationId = correlationId,
            ActivityHandle = bookmark.ActivityInstanceId != null ? ActivityHandle.FromActivityInstanceId(bookmark.ActivityInstanceId) : null,
            BookmarkId = bookmark.Id
        };

        await ExecuteWorkflowAsync(httpContext, workflowGraph, runWorkflowParams, bookmarkPayload, workflowInstance, input);
    }

    private async Task ExecuteWorkflowAsync(HttpContext httpContext, WorkflowGraph workflowGraph, RunWorkflowOptions workflowOptions, HttpEndpointBookmarkPayload bookmarkPayload, WorkflowInstance? workflowInstance, IDictionary<string, object> input)
    {
        var serviceProvider = httpContext.RequestServices;
        var cancellationToken = httpContext.RequestAborted;
        var workflow = workflowGraph.Workflow;

        var workflowRunner = serviceProvider.GetRequiredService<IWorkflowRunner>();
        try
        {
            var result = await ExecuteWithinTimeoutAsync<RunWorkflowResult?>(async ct =>
            {
                if (workflowInstance != null)
                    return await workflowRunner.RunAsync(workflow, workflowInstance.WorkflowState, workflowOptions, ct);

                var gate = serviceProvider.GetRequiredService<IWorkflowActivationGate>();
                await using var lease = await gate.EvaluateAsync(workflow, workflowOptions.CorrelationId, ct);
                if (!lease.CanStart)
                {
                    httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
                    return null;
                }

                workflowOptions.RequireNewInstance = true;
                return await workflowRunner.RunAsync(workflowGraph, workflowOptions, lease.GetEffectiveCancellationToken(ct));
            }, bookmarkPayload.RequestTimeout, httpContext);
            if (result != null)
                await HandleWorkflowFaultAsync(serviceProvider, httpContext, result, cancellationToken);
        }
        catch (WorkflowInstanceAlreadyExistsException) when (workflowInstance == null && !httpContext.Response.HasStarted)
        {
            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        }
    }

    private async Task<T> ExecuteWithinTimeoutAsync<T>(Func<CancellationToken, Task<T>> action, TimeSpan? requestTimeout, HttpContext httpContext)
    {
        // If no request timeout is specified, execute the action without any timeout.
        if (requestTimeout == null)
            return await action(httpContext.RequestAborted);

        // Create a combined cancellation token that cancels when the request is aborted or when the request timeout is reached.
        using var requestTimeoutCancellationTokenSource = new CancellationTokenSource();
        requestTimeoutCancellationTokenSource.CancelAfter(requestTimeout.Value);
        using var combinedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(httpContext.RequestAborted, requestTimeoutCancellationTokenSource.Token);
        var originalCancellationToken = httpContext.RequestAborted;

        // Replace the original cancellation token with the combined one.
        httpContext.RequestAborted = combinedTokenSource.Token;

        // Execute the action.
        var result = await action(httpContext.RequestAborted);

        // Restore the original cancellation token.
        httpContext.RequestAborted = originalCancellationToken;

        return result;
    }

    private HttpRouteData GetMatchingRoute(IServiceProvider serviceProvider, string path)
    {
        var routeMatcher = serviceProvider.GetRequiredService<IRouteMatcher>();
        var routeTable = serviceProvider.GetRequiredService<IRouteTable>();

        var matchingRouteQuery =
            from routeData in routeTable
            let routeValues = routeMatcher.Match(routeData.Route, path)
            where routeValues != null
            select new
            {
                route = routeData,
                routeValues
            };

        var matchingRoute = matchingRouteQuery.FirstOrDefault();

        return matchingRoute?.route ?? new HttpRouteData(path);
    }

    private static string? TryStripBasePath(string route, string? basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath) || basePath == "/")
            return route;

        var routeSegments = GetRouteSegments(route);
        var basePathSegments = GetRouteSegments(basePath);
        var basePathIndex = FindSegmentSequence(routeSegments, basePathSegments);

        if (basePathIndex != 0)
            return null;

        var remainingSegments = routeSegments.Skip(basePathIndex + basePathSegments.Length);
        return remainingSegments.Any() ? $"/{string.Join('/', remainingSegments)}" : "/";
    }

    /// <summary>
    /// Returns 503 with Retry-After while paused or draining, without starting or resuming a workflow.
    /// Default hosts call this before lookup. Combined hosts call it after a trigger or bookmark match.
    /// </summary>
    private static bool TryRejectWhenNotAcceptingWork(HttpContext httpContext, IServiceProvider serviceProvider)
    {
        var quiescenceSignal = serviceProvider.GetService<IQuiescenceSignal>();
        if (quiescenceSignal is null || quiescenceSignal.IsAcceptingNewWork)
        {
            return false;
        }

        httpContext.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
        // Retry-After is reason-aware: drain is short (host is exiting and will be replaced shortly), but an
        // administrative pause is indefinite, so a longer back-off avoids a tight retry loop while operators
        // perform maintenance.
        httpContext.Response.Headers.RetryAfter = quiescenceSignal.CurrentState.Reason.HasFlag(QuiescenceReason.Drain) ? "5" : "60";
        return true;
    }

    private static bool IsBasePathMatch(string route, string basePath) =>
        basePath == "/" ||
        string.Equals(route, basePath, StringComparison.OrdinalIgnoreCase) ||
        route.StartsWith($"{basePath}/", StringComparison.OrdinalIgnoreCase);

    private static int FindSegmentSequence(string[] routeSegments, string[] candidateSegments)
    {
        if (candidateSegments.Length == 0)
            return 0;

        for (var startIndex = 0; startIndex <= routeSegments.Length - candidateSegments.Length; startIndex++)
        {
            var isMatch = true;

            for (var candidateIndex = 0; candidateIndex < candidateSegments.Length; candidateIndex++)
            {
                if (!string.Equals(routeSegments[startIndex + candidateIndex], candidateSegments[candidateIndex], StringComparison.OrdinalIgnoreCase))
                {
                    isMatch = false;
                    break;
                }
            }

            if (isMatch)
                return startIndex;
        }

        return -1;
    }

    private static string[] GetRouteSegments(string route) => route.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);

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

    [RequiresUnreferencedCode("Calls System.Text.Json.JsonSerializer.Serialize<TValue>(TValue, JsonSerializerOptions)")]
    private static async Task HandleMultipleWorkflowsFoundAsync(HttpContext httpContext, CancellationToken cancellationToken)
    {
        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var responseContent = JsonSerializer.Serialize(new
        {
            errorMessage = "The call is ambiguous and matches multiple workflows.",
            correlationId = httpContext.TraceIdentifier
        });

        await httpContext.Response.WriteAsync(responseContent, cancellationToken);
    }

    private async Task<bool> HandleWorkflowFaultAsync(IServiceProvider serviceProvider, HttpContext httpContext, RunWorkflowResult workflowExecutionResult, CancellationToken cancellationToken)
    {
        if (!workflowExecutionResult.WorkflowState.Incidents.Any() || httpContext.Response.HasStarted)
            return false;

        var httpEndpointFaultHandler = serviceProvider.GetRequiredService<IHttpEndpointFaultHandler>();
        var workflowInstanceManager = serviceProvider.GetRequiredService<IWorkflowInstanceManager>();
        var workflowState = (await workflowInstanceManager.FindByIdAsync(workflowExecutionResult.WorkflowState.Id, cancellationToken))!;
        await httpEndpointFaultHandler.HandleAsync(new(httpContext, workflowState.WorkflowState, cancellationToken));
        return true;
    }

    private async Task<bool> TryAuthorizeAsync(HttpContext httpContext, Workflow workflow, HttpEndpointBookmarkPayload bookmarkPayload)
    {
        if (bookmarkPayload.Authorize == false)
            return true;

        var handler = httpContext.RequestServices.GetRequiredService<IHttpEndpointAuthorizationHandler>();
        if (await handler.AuthorizeAsync(new(httpContext, workflow, bookmarkPayload.Policy)))
            return true;

        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return false;
    }

    private string ComputeBookmarkHash(IServiceProvider serviceProvider, string path, string method)
    {
        var bookmarkPayload = new HttpEndpointBookmarkPayload(path, method);
        var bookmarkHasher = serviceProvider.GetRequiredService<IStimulusHasher>();
        return bookmarkHasher.Hash(HttpStimulusNames.HttpEndpoint, bookmarkPayload);
    }
}
