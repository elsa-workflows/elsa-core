using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Permissions;
using Elsa.UserTasks.Services;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Elsa.UserTasks.Endpoints;

/// <summary>
/// Shared behavior for the authenticated task endpoints: resolve the actor once, and translate a domain
/// conflict code into the single canonical HTTP shape.
/// </summary>
internal static class UserTaskEndpointHelpers
{
    public static UserTaskQueryScopeKind ParseScope(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "available" => UserTaskQueryScopeKind.Available,
        "history" => UserTaskQueryScopeKind.History,
        "all" => UserTaskQueryScopeKind.All,
        "needs-attention" or "needsattention" => UserTaskQueryScopeKind.NeedsAttention,
        _ => UserTaskQueryScopeKind.Assigned
    };

    /// <summary>Unknown status values are dropped rather than rejected, so a stale bookmark still loads.</summary>
    public static IReadOnlyCollection<UserTaskStatus> ParseStatuses(IEnumerable<string>? values) => values == null
        ? []
        : values.Select(value => Enum.TryParse<UserTaskStatus>(value, ignoreCase: true, out var parsed) ? parsed : (UserTaskStatus?)null)
            .Where(x => x != null)
            .Select(x => x!.Value)
            .Distinct()
            .ToArray();

    public static UserTaskQuery ApplyDueFilter(UserTaskQuery query, string? due, DateTimeOffset now) => due?.Trim().ToLowerInvariant() switch
    {
        "overdue" => query with { OnlyOverdue = true },
        "nodueDate" or "nodue" or "nodate" => query with { OnlyWithoutDueDate = true },
        "today" => query with { DueFrom = now.Date, DueTo = now.Date.AddDays(1).AddTicks(-1) },
        "thisweek" => query with { DueFrom = now.Date, DueTo = now.Date.AddDays(7).AddTicks(-1) },
        _ => query
    };

    public static UserTaskParticipantType? ParseParticipantType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "group" => UserTaskParticipantType.Group,
        "user" => UserTaskParticipantType.User,
        _ => null
    };
}

internal abstract class UserTaskEndpointBase<TRequest, TResponse> : ElsaEndpoint<TRequest, TResponse>
    where TRequest : notnull, new()
    where TResponse : notnull
{
    protected async Task SendConflictAsync(string code, CancellationToken cancellationToken) =>
        await HttpContext.Response.SendAsync(UserTaskErrors.Describe(code), UserTaskErrors.StatusCodeFor(code), cancellation: cancellationToken);

    /// <summary>
    /// Sends the canonical command envelope. Terminal commands answer <c>202</c> because the workflow resumes
    /// out of band; clients observe the final state through requery or invalidation.
    /// </summary>
    protected async Task SendOperationAsync(UserTaskOperationResult result, UserTaskActor actor, IUserTaskAccessPolicy policy, bool accepted, CancellationToken cancellationToken)
    {
        if (!result.Accepted)
        {
            await SendConflictAsync(result.ConflictCode ?? "conflict", cancellationToken);
            return;
        }

        var summary = await UserTaskModelMapper.ToSummaryAsync(result.Task, actor, policy, cancellationToken);
        var response = new UserTaskOperationResponse(result.Operation.OperationId, accepted ? "accepted" : "completed", result.Task.Revision, summary);
        await HttpContext.Response.SendAsync(response, accepted ? StatusCodes.Status202Accepted : StatusCodes.Status200OK, cancellation: cancellationToken);
    }
}

internal sealed class FeatureCapabilitiesEndpoint(IUserTaskIdentityResolver identityResolver, IOptions<UserTasksOptions> options, IUserTaskParticipantDirectory directory)
    : ElsaEndpointWithoutRequest<UserTaskFeatureCapabilities>
{
    public override void Configure()
    {
        Get("/user-tasks/capabilities");
        ConfigurePermissions(UserTasksPermissions.Read);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }

        var settings = options.Value;
        var isManager = actor.IsManager && actor.HasPermission(UserTasksPermissions.Manage);
        // The descriptor is advisory: it decides what the client renders, never what the server allows.
        await Send.OkAsync(new UserTaskFeatureCapabilities
        {
            Enabled = true,
            CanList = actor.HasPermission(UserTasksPermissions.Read),
            CanRead = actor.HasPermission(UserTasksPermissions.Read),
            CanReadAll = isManager,
            CanClaim = actor.HasPermission(UserTasksPermissions.Claim),
            CanRelease = actor.HasPermission(UserTasksPermissions.Claim),
            CanComplete = actor.HasPermission(UserTasksPermissions.Complete),
            CanAssign = actor.HasPermission(UserTasksPermissions.Assign),
            CanUpdate = actor.HasPermission(UserTasksPermissions.Update),
            CanCancel = actor.HasPermission(UserTasksPermissions.Cancel),
            CanCreateGuestLinks = actor.HasPermission(UserTasksPermissions.Invite),
            CanViewProtected = actor.HasPermission(UserTasksPermissions.Read),
            ParticipantPicker = actor.HasPermission(UserTasksPermissions.LookupParticipants) && directory is not EmptyUserTaskParticipantDirectory,
            Realtime = settings.RealtimeEnabled,
            PollingIntervalSeconds = settings.PollingIntervalSeconds
        }, cancellationToken);
    }
}

internal sealed class ListEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, ISystemClock clock)
    : ElsaEndpoint<ListUserTasksRequest, UserTaskListResponse>
{
    public override void Configure()
    {
        Get("/user-tasks");
        ConfigurePermissions(UserTasksPermissions.Read);
    }

    public override async Task HandleAsync(ListUserTasksRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }

        var query = new UserTaskQuery
        {
            TenantId = actor.Subject.TenantId,
            Cursor = request.Cursor,
            Limit = request.Limit,
            Sort = request.Sort ?? "created",
            Descending = string.Equals(request.Direction, "desc", StringComparison.OrdinalIgnoreCase),
            Statuses = UserTaskEndpointHelpers.ParseStatuses(request.Status),
            PriorityFrom = request.PriorityFrom,
            PriorityTo = request.PriorityTo,
            DueFrom = request.From,
            DueTo = request.To,
            WorkflowDefinitionId = request.WorkflowDefinitionId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            Reference = request.Reference,
            TaskType = request.TaskType,
            Search = request.Search,
            IncludeTotalCount = request.IncludeTotalCount
        };
        query = UserTaskEndpointHelpers.ApplyDueFilter(query, request.Due, clock.UtcNow);

        var scope = UserTaskEndpointHelpers.ParseScope(request.Scope);
        var result = await manager.QueryAsync(query, scope, actor, cancellationToken);
        if (result == null)
        {
            // The requested scope is not available to this actor (for example a non-manager asking for
            // `all`). This is an authorization outcome, not an empty page.
            await Send.ForbiddenAsync(cancellationToken);
            return;
        }

        await Send.OkAsync(new UserTaskListResponse(result.Items, result.NextCursor, result.TotalCount), cancellationToken);
    }
}

internal sealed class GetEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver) : ElsaEndpointWithoutRequest<UserTaskDetail>
{
    public override void Configure()
    {
        Get("/user-tasks/{taskId}");
        ConfigurePermissions(UserTasksPermissions.Read);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        var task = actor == null ? null : await manager.GetAsync(actor.Subject.TenantId, Route<string>("taskId")!, actor, cancellationToken);
        if (task == null)
        {
            // Concealment is deliberate: an unauthorized caller must not be able to tell an existing task
            // from a missing one.
            await Send.NotFoundAsync(cancellationToken);
            return;
        }
        await Send.OkAsync(task, cancellationToken);
    }
}

internal sealed class CapabilitiesEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver) : ElsaEndpointWithoutRequest<UserTaskCapabilities>
{
    public override void Configure()
    {
        Get("/user-tasks/{taskId}/capabilities");
        ConfigurePermissions(UserTasksPermissions.Read);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        var result = actor == null ? null : await manager.GetCapabilitiesAsync(actor.Subject.TenantId, Route<string>("taskId")!, actor, cancellationToken);
        if (result == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }
        await Send.OkAsync(result, cancellationToken);
    }
}

internal sealed class ListEventsEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver) : ElsaEndpointWithoutRequest<UserTaskEventsResult>
{
    public override void Configure()
    {
        Get("/user-tasks/{taskId}/events");
        ConfigurePermissions(UserTasksPermissions.Read);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        var result = actor == null
            ? null
            : await manager.GetEventsAsync(actor.Subject.TenantId, Route<string>("taskId")!, Query<string>("cursor", isRequired: false),
                Query<int?>("limit", isRequired: false) ?? 50, actor, cancellationToken);
        if (result == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }
        await Send.OkAsync(result, cancellationToken);
    }
}

internal sealed class ClaimEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy)
    : UserTaskEndpointBase<UserTaskMutationApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/claim");
        ConfigurePermissions(UserTasksPermissions.Claim);
    }

    public override async Task HandleAsync(UserTaskMutationApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }
        var result = await manager.ClaimAsync(actor.Subject.TenantId, Route<string>("taskId")!, new(request.ExpectedRevision, request.OperationId), actor, cancellationToken);
        await SendOperationAsync(result, actor, policy, accepted: false, cancellationToken);
    }
}

internal sealed class ReleaseEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy)
    : UserTaskEndpointBase<UserTaskMutationApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/release");
        ConfigurePermissions(UserTasksPermissions.Claim);
    }

    public override async Task HandleAsync(UserTaskMutationApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }
        var result = await manager.ReleaseAsync(actor.Subject.TenantId, Route<string>("taskId")!, new(request.ExpectedRevision, request.OperationId), actor, cancellationToken);
        await SendOperationAsync(result, actor, policy, accepted: false, cancellationToken);
    }
}

internal sealed class AssignEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy)
    : UserTaskEndpointBase<AssignUserTaskApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/assign");
        ConfigurePermissions(UserTasksPermissions.Assign);
    }

    public override async Task HandleAsync(AssignUserTaskApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }
        if (request.Assignee is null || string.IsNullOrWhiteSpace(request.Assignee.Id))
        {
            await SendConflictAsync("invalid-action", cancellationToken);
            return;
        }

        // The tenant is taken from the caller's own scope and never from the body, so a cross-tenant
        // reference cannot be constructed over the wire at all.
        var assignee = new ParticipantReference(
            actor.Subject.TenantId,
            string.IsNullOrWhiteSpace(request.Assignee.Provider) ? actor.Subject.Provider : request.Assignee.Provider!,
            string.Equals(request.Assignee.Kind, UserTaskParticipantSummary.GroupKind, StringComparison.OrdinalIgnoreCase) ? UserTaskParticipantType.Group : UserTaskParticipantType.User,
            request.Assignee.Id,
            request.Assignee.DisplayName);
        var result = await manager.AssignAsync(actor.Subject.TenantId, Route<string>("taskId")!,
            new(request.ExpectedRevision, assignee, request.Reason, request.OperationId), actor, cancellationToken);
        await SendOperationAsync(result, actor, policy, accepted: false, cancellationToken);
    }
}

internal sealed class ScheduleEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy)
    : UserTaskEndpointBase<ScheduleUserTaskApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Patch("/user-tasks/{taskId}");
        ConfigurePermissions(UserTasksPermissions.Update);
    }

    public override async Task HandleAsync(ScheduleUserTaskApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }
        var result = await manager.UpdateSchedulingAsync(actor.Subject.TenantId, Route<string>("taskId")!,
            new(request.ExpectedRevision, request.Priority, request.DueAt, request.OperationId), actor, cancellationToken);
        await SendOperationAsync(result, actor, policy, accepted: false, cancellationToken);
    }
}

internal sealed class CompleteEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy)
    : UserTaskEndpointBase<CompleteUserTaskApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/complete");
        ConfigurePermissions(UserTasksPermissions.Complete);
    }

    public override async Task HandleAsync(CompleteUserTaskApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }
        if (string.IsNullOrWhiteSpace(request.OperationId))
        {
            await SendConflictAsync("invalid-action", cancellationToken);
            return;
        }
        var result = await manager.CompleteAsync(actor.Subject.TenantId, Route<string>("taskId")!,
            new(request.ExpectedRevision, request.OperationId, request.ActionKey, request.Data), actor, cancellationToken);
        await SendOperationAsync(result, actor, policy, accepted: true, cancellationToken);
    }
}

internal sealed class CancelEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy)
    : UserTaskEndpointBase<CancelUserTaskApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/cancel");
        ConfigurePermissions(UserTasksPermissions.Cancel);
    }

    public override async Task HandleAsync(CancelUserTaskApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }
        var result = await manager.CancelAsync(actor.Subject.TenantId, Route<string>("taskId")!,
            new(request.ExpectedRevision, request.OperationId, request.Reason), actor, cancellationToken);
        await SendOperationAsync(result, actor, policy, accepted: true, cancellationToken);
    }
}

internal sealed class RetryResolutionEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy)
    : UserTaskEndpointBase<UserTaskMutationApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/retry-resolution");
        ConfigurePermissions(UserTasksPermissions.Manage);
    }

    public override async Task HandleAsync(UserTaskMutationApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }
        var result = await manager.RetryResolutionAsync(actor.Subject.TenantId, Route<string>("taskId")!, new(request.ExpectedRevision, request.OperationId), actor, cancellationToken);
        await SendOperationAsync(result, actor, policy, accepted: true, cancellationToken);
    }
}

internal sealed class RevealFieldEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver)
    : ElsaEndpoint<RevealUserTaskFieldApiRequest, RevealUserTaskFieldResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/reveal");
        ConfigurePermissions(UserTasksPermissions.Read);
    }

    public override async Task HandleAsync(RevealUserTaskFieldApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        var value = actor == null || string.IsNullOrWhiteSpace(request.FieldKey)
            ? null
            : await manager.RevealFieldAsync(actor.Subject.TenantId, Route<string>("taskId")!, request.FieldKey, actor, cancellationToken);
        if (value == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }
        await Send.OkAsync(new RevealUserTaskFieldResponse(request.FieldKey, value), cancellationToken);
    }
}

internal sealed class IssueInvitationEndpoint(IUserTaskInvitationService invitations, IUserTaskIdentityResolver identityResolver)
    : ElsaEndpoint<IssueUserTaskInvitationApiRequest, UserTaskInvitationIssueResult>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/invitations");
        ConfigurePermissions(UserTasksPermissions.Invite);
    }

    public override async Task HandleAsync(IssueUserTaskInvitationApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }

        var result = await invitations.IssueAsync(actor.Subject.TenantId, Route<string>("taskId")!,
            new(request.ExpectedRevision, request.VerifierName, request.AllowedActions,
                request.Recipient, request.Lifetime, request.OperationId), actor, cancellationToken);
        if (result == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }
        // The response carries metadata only. The secret leaves through the dispatcher, never the API.
        await HttpContext.Response.SendAsync(result, StatusCodes.Status201Created, cancellation: cancellationToken);
    }
}

internal sealed class ListInvitationsEndpoint(IUserTaskInvitationService invitations, IUserTaskIdentityResolver identityResolver)
    : ElsaEndpointWithoutRequest<UserTaskInvitationListResponse>
{
    public override void Configure()
    {
        Get("/user-tasks/{taskId}/invitations");
        ConfigurePermissions(UserTasksPermissions.Invite);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        var result = actor == null ? null : await invitations.ListAsync(actor.Subject.TenantId, Route<string>("taskId")!, actor, cancellationToken);
        if (result == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }
        await Send.OkAsync(new UserTaskInvitationListResponse(result), cancellationToken);
    }
}

internal sealed class RevokeInvitationEndpoint(IUserTaskInvitationService invitations, IUserTaskIdentityResolver identityResolver)
    : ElsaEndpoint<UserTaskMutationApiRequest>
{
    public override void Configure()
    {
        Delete("/user-tasks/{taskId}/invitations/{invitationId}");
        ConfigurePermissions(UserTasksPermissions.Invite);
    }

    public override async Task HandleAsync(UserTaskMutationApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }
        var revoked = await invitations.RevokeAsync(actor.Subject.TenantId, Route<string>("taskId")!, Route<string>("invitationId")!, request.ExpectedRevision, actor, cancellationToken);
        if (!revoked)
            await Send.NotFoundAsync(cancellationToken);
        else
            await Send.NoContentAsync(cancellationToken);
    }
}

internal sealed class ListParticipantsEndpoint(IUserTaskParticipantDirectory directory, IUserTaskIdentityResolver identityResolver)
    : ElsaEndpoint<UserTaskParticipantLookupApiRequest, UserTaskParticipantSearchResponse>
{
    public override void Configure()
    {
        Get("/user-task-participants");
        ConfigurePermissions(UserTasksPermissions.LookupParticipants);
    }

    public override async Task HandleAsync(UserTaskParticipantLookupApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }

        // A host without a directory answers with an empty page rather than an identity-module error, so the
        // picker degrades to a plain reference editor instead of breaking the page.
        var result = await directory.SearchAsync(new(actor.Subject.TenantId, request.Search,
            UserTaskEndpointHelpers.ParseParticipantType(request.Type), request.Cursor,
            Math.Clamp(request.Limit <= 0 ? 50 : request.Limit, 1, 200)), cancellationToken);
        var items = result.Items
            .Where(x => string.Equals(x.TenantId, actor.Subject.TenantId, StringComparison.Ordinal))
            .Select(x => UserTaskParticipantSummary.From(x)!)
            .ToArray();
        await Send.OkAsync(new UserTaskParticipantSearchResponse(items, result.NextCursor), cancellationToken);
    }
}

/// <summary>
/// Anonymous invitation surface. Both endpoints are rate limited per caller and answer with the same generic
/// shape for valid and invalid tokens.
/// </summary>
internal abstract class AnonymousInvitationEndpointBase<TRequest, TResponse> : Endpoint<TRequest, TResponse>
    where TRequest : notnull, new()
    where TResponse : notnull
{
    protected async Task<bool> TryAcquireAsync(IUserTaskInvitationRateLimiter limiter, CancellationToken cancellationToken)
    {
        // Partition by remote address, not by token: probing many tokens from one host drains one budget.
        var partition = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        if (await limiter.TryAcquireAsync(partition, cancellationToken))
            return true;

        await HttpContext.Response.SendAsync(UserTaskErrors.Describe("invitation-unavailable"), StatusCodes.Status429TooManyRequests, cancellation: cancellationToken);
        return false;
    }
}

internal sealed class DescribeInvitationEndpoint(IUserTaskInvitationService invitations, IUserTaskInvitationRateLimiter limiter)
    : AnonymousInvitationEndpointBase<EmptyRequest, UserTaskInvitationChallengeDescriptor>
{
    public override void Configure()
    {
        Get("/user-task-invitations/{token}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(EmptyRequest request, CancellationToken cancellationToken)
    {
        if (!await TryAcquireAsync(limiter, cancellationToken))
            return;
        await Send.OkAsync(await invitations.DescribeAsync(Route<string>("token")!, cancellationToken), cancellationToken);
    }
}

internal sealed class VerifyInvitationEndpoint(IUserTaskInvitationService invitations, IUserTaskInvitationRateLimiter limiter)
    : AnonymousInvitationEndpointBase<VerifyUserTaskInvitationApiRequest, UserTaskGuestSessionResponse>
{
    public override void Configure()
    {
        Post("/user-task-invitations/{token}/verify");
        AllowAnonymous();
    }

    public override async Task HandleAsync(VerifyUserTaskInvitationApiRequest request, CancellationToken cancellationToken)
    {
        if (!await TryAcquireAsync(limiter, cancellationToken))
            return;

        var result = await invitations.VerifyAsync(new(Route<string>("token")!, request.Code, request.State), cancellationToken);
        if (!result.Succeeded)
        {
            // One shape for missing, expired, consumed, revoked, and wrong-code. No existence oracle.
            await HttpContext.Response.SendAsync(UserTaskErrors.Describe("invitation-unavailable"), StatusCodes.Status400BadRequest, cancellation: cancellationToken);
            return;
        }
        await Send.OkAsync(new UserTaskGuestSessionResponse(result.SessionToken, result.TaskId, result.ExpiresAt), cancellationToken);
    }
}

/// <summary>
/// Guest task surface. The presented session credential identifies the task, so no task ID appears in the
/// route and a guest can never address a task other than the one their invitation was issued for.
/// </summary>
internal sealed class GuestTaskEndpoint(IUserTaskManager manager, UserTaskGuestActorResolver guestResolver)
    : EndpointWithoutRequest<UserTaskDetail>
{
    public override void Configure()
    {
        Get("/user-task-sessions/current");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var actor = await guestResolver.ResolveAsync(UserTaskGuestActorResolver.ReadCredential(HttpContext.Request.Headers.Authorization), cancellationToken);
        if (actor?.GuestTaskId == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }

        var detail = await manager.GetAsync(actor.Subject.TenantId, actor.GuestTaskId, actor, cancellationToken);
        if (detail == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }
        await Send.OkAsync(detail, cancellationToken);
    }
}

internal sealed class GuestCompleteEndpoint(IUserTaskManager manager, UserTaskGuestActorResolver guestResolver, IUserTaskAccessPolicy policy)
    : Endpoint<CompleteUserTaskApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-task-sessions/current/complete");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CompleteUserTaskApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await guestResolver.ResolveAsync(UserTaskGuestActorResolver.ReadCredential(HttpContext.Request.Headers.Authorization), cancellationToken);
        if (actor?.GuestTaskId == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return;
        }
        if (string.IsNullOrWhiteSpace(request.OperationId))
        {
            await HttpContext.Response.SendAsync(UserTaskErrors.Describe("invalid-action"), StatusCodes.Status422UnprocessableEntity, cancellation: cancellationToken);
            return;
        }

        var result = await manager.CompleteAsync(actor.Subject.TenantId, actor.GuestTaskId,
            new(request.ExpectedRevision, request.OperationId, request.ActionKey, request.Data), actor, cancellationToken);
        if (!result.Accepted)
        {
            var code = result.ConflictCode ?? "conflict";
            await HttpContext.Response.SendAsync(UserTaskErrors.Describe(code), UserTaskErrors.StatusCodeFor(code), cancellation: cancellationToken);
            return;
        }

        var summary = await UserTaskModelMapper.ToSummaryAsync(result.Task, actor, policy, cancellationToken);
        await HttpContext.Response.SendAsync(new UserTaskOperationResponse(result.Operation.OperationId, "accepted", result.Task.Revision, summary),
            StatusCodes.Status202Accepted, cancellation: cancellationToken);
    }
}
