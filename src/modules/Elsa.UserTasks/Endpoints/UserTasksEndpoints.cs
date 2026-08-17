using Elsa.Abstractions;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Permissions;
using Elsa.UserTasks.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.UserTasks.Endpoints;

internal sealed class ListEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver) : ElsaEndpoint<ListUserTasksRequest, UserTaskQueryResultDto>
{
    public override void Configure()
    {
        Get("/user-tasks");
        ConfigurePermissions(UserTasksPermissions.Read);
    }

    public override async Task<UserTaskQueryResultDto> ExecuteAsync(ListUserTasksRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return new UserTaskQueryResultDto([], null, null);
        }
        return await manager.QueryAsync(new UserTaskQuery
        {
            TenantId = actor.Subject.TenantId,
            Cursor = request.Cursor,
            Limit = request.Limit,
            Sort = request.Sort ?? "created",
            Descending = string.Equals(request.Direction, "desc", StringComparison.OrdinalIgnoreCase),
            Status = request.Status,
            PriorityFrom = request.PriorityFrom,
            PriorityTo = request.PriorityTo,
            DueFrom = request.DueFrom,
            DueTo = request.DueTo,
            WorkflowDefinitionId = request.WorkflowDefinitionId,
            WorkflowInstanceId = request.WorkflowInstanceId,
            Reference = request.Reference,
            TaskType = request.TaskType,
            Search = request.Search,
            IncludeTotalCount = request.IncludeTotalCount
        }, actor, cancellationToken);
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

internal sealed class ClaimEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy) : ElsaEndpoint<UserTaskMutationApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/claim");
        ConfigurePermissions(UserTasksPermissions.Claim);
    }

    public override async Task<UserTaskOperationResponse> ExecuteAsync(UserTaskMutationApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken) ?? throw new UnauthorizedAccessException();
        var result = await manager.ClaimAsync(actor.Subject.TenantId, Route<string>("taskId")!, new UserTaskMutationRequest(request.ExpectedRevision, request.OperationId), actor, cancellationToken);
        return await MapAsync(result, actor, policy, cancellationToken);
    }

    private static async Task<UserTaskOperationResponse> MapAsync(UserTaskOperationResult result, UserTaskActor actor, IUserTaskAccessPolicy policy, CancellationToken cancellationToken)
    {
        var summary = await UserTaskModelMapper.ToSummaryAsync(result.Task, actor, policy, cancellationToken);
        return new UserTaskOperationResponse(summary, result.Operation.OperationId, result.Accepted, result.ConflictCode);
    }
}

internal sealed class ReleaseEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy) : ElsaEndpoint<UserTaskMutationApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/release");
        ConfigurePermissions(UserTasksPermissions.Claim);
    }

    public override async Task<UserTaskOperationResponse> ExecuteAsync(UserTaskMutationApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken) ?? throw new UnauthorizedAccessException();
        var result = await manager.ReleaseAsync(actor.Subject.TenantId, Route<string>("taskId")!, new UserTaskMutationRequest(request.ExpectedRevision, request.OperationId), actor, cancellationToken);
        var summary = await UserTaskModelMapper.ToSummaryAsync(result.Task, actor, policy, cancellationToken);
        return new UserTaskOperationResponse(summary, result.Operation.OperationId, result.Accepted, result.ConflictCode);
    }
}

internal sealed class AssignEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy) : ElsaEndpoint<AssignUserTaskApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/assign");
        ConfigurePermissions(UserTasksPermissions.Assign);
    }

    public override async Task<UserTaskOperationResponse> ExecuteAsync(AssignUserTaskApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken) ?? throw new UnauthorizedAccessException();
        var result = await manager.AssignAsync(actor.Subject.TenantId, Route<string>("taskId")!, new UserTaskAssignRequest(request.ExpectedRevision, request.Assignee, request.Reason, request.OperationId), actor, cancellationToken);
        var summary = await UserTaskModelMapper.ToSummaryAsync(result.Task, actor, policy, cancellationToken);
        return new UserTaskOperationResponse(summary, result.Operation.OperationId, result.Accepted, result.ConflictCode);
    }
}

internal sealed class ScheduleEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy) : ElsaEndpoint<ScheduleUserTaskApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Patch("/user-tasks/{taskId}/scheduling");
        ConfigurePermissions(UserTasksPermissions.Update);
    }

    public override async Task<UserTaskOperationResponse> ExecuteAsync(ScheduleUserTaskApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken) ?? throw new UnauthorizedAccessException();
        var result = await manager.UpdateSchedulingAsync(actor.Subject.TenantId, Route<string>("taskId")!, new UserTaskSchedulingUpdate(request.ExpectedRevision, request.Priority, request.DueAt, request.OperationId), actor, cancellationToken);
        var summary = await UserTaskModelMapper.ToSummaryAsync(result.Task, actor, policy, cancellationToken);
        return new UserTaskOperationResponse(summary, result.Operation.OperationId, result.Accepted, result.ConflictCode);
    }
}

internal sealed class CompleteEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy) : ElsaEndpoint<CompleteUserTaskApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/complete");
        ConfigurePermissions(UserTasksPermissions.Complete);
    }

    public override async Task<UserTaskOperationResponse> ExecuteAsync(CompleteUserTaskApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken) ?? throw new UnauthorizedAccessException();
        var result = await manager.CompleteAsync(actor.Subject.TenantId, Route<string>("taskId")!, new UserTaskCompletionRequest(request.ExpectedRevision, request.OperationId, request.ActionKey, request.Data), actor, cancellationToken);
        var summary = await UserTaskModelMapper.ToSummaryAsync(result.Task, actor, policy, cancellationToken);
        return new UserTaskOperationResponse(summary, result.Operation.OperationId, result.Accepted, result.ConflictCode);
    }
}

internal sealed class CancelEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy) : ElsaEndpoint<CancelUserTaskApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/cancel");
        ConfigurePermissions(UserTasksPermissions.Cancel);
    }

    public override async Task<UserTaskOperationResponse> ExecuteAsync(CancelUserTaskApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken) ?? throw new UnauthorizedAccessException();
        var result = await manager.CancelAsync(actor.Subject.TenantId, Route<string>("taskId")!, new UserTaskCancelRequest(request.ExpectedRevision, request.OperationId, request.Reason), actor, cancellationToken);
        var summary = await UserTaskModelMapper.ToSummaryAsync(result.Task, actor, policy, cancellationToken);
        return new UserTaskOperationResponse(summary, result.Operation.OperationId, result.Accepted, result.ConflictCode);
    }
}

internal sealed class RetryResolutionEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver, IUserTaskAccessPolicy policy) : ElsaEndpoint<UserTaskMutationApiRequest, UserTaskOperationResponse>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/retry-resolution");
        ConfigurePermissions(UserTasksPermissions.Manage);
    }

    public override async Task<UserTaskOperationResponse> ExecuteAsync(UserTaskMutationApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken) ?? throw new UnauthorizedAccessException();
        var result = await manager.RetryResolutionAsync(actor.Subject.TenantId, Route<string>("taskId")!, new UserTaskMutationRequest(request.ExpectedRevision, request.OperationId), actor, cancellationToken);
        var summary = await UserTaskModelMapper.ToSummaryAsync(result.Task, actor, policy, cancellationToken);
        return new UserTaskOperationResponse(summary, result.Operation.OperationId, result.Accepted, result.ConflictCode);
    }
}

internal sealed class IssueInvitationEndpoint(IUserTaskInvitationService invitations, IUserTaskIdentityResolver identityResolver) : ElsaEndpoint<IssueUserTaskInvitationApiRequest, UserTaskInvitationIssueResult>
{
    public override void Configure()
    {
        Post("/user-tasks/{taskId}/invitations");
        ConfigurePermissions(UserTasksPermissions.Invite);
    }

    public override async Task<UserTaskInvitationIssueResult> ExecuteAsync(IssueUserTaskInvitationApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return default!;
        }

        var result = await invitations.IssueAsync(actor.Subject.TenantId, Route<string>("taskId")!,
            new UserTaskInvitationIssueRequest(request.ExpectedRevision, request.VerifierName, request.AllowedActions,
                request.Recipient, request.Lifetime, request.OperationId), actor, cancellationToken);
        if (result == null)
            await Send.NotFoundAsync(cancellationToken);
        return result ?? default!;
    }
}

internal sealed class ListInvitationsEndpoint(IUserTaskInvitationService invitations, IUserTaskIdentityResolver identityResolver) : ElsaEndpointWithoutRequest<IReadOnlyCollection<UserTaskInvitationSummary>>
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
        await Send.OkAsync(result, cancellationToken);
    }
}

internal sealed class RevokeInvitationEndpoint(IUserTaskInvitationService invitations, IUserTaskIdentityResolver identityResolver) : ElsaEndpoint<UserTaskMutationApiRequest, bool>
{
    public override void Configure()
    {
        Delete("/user-tasks/{taskId}/invitations/{invitationId}");
        ConfigurePermissions(UserTasksPermissions.Invite);
    }

    public override async Task<bool> ExecuteAsync(UserTaskMutationApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return false;
        }
        var revoked = await invitations.RevokeAsync(actor.Subject.TenantId, Route<string>("taskId")!, Route<string>("invitationId")!, request.ExpectedRevision, actor, cancellationToken);
        if (!revoked)
            await Send.NotFoundAsync(cancellationToken);
        return revoked;
    }
}

internal sealed class VerifyInvitationEndpoint(IUserTaskInvitationService invitations) : ElsaEndpoint<VerifyUserTaskInvitationApiRequest, UserTaskInvitationVerificationResultWithSession>
{
    public override void Configure()
    {
        Post("/user-task-invitations/{token}/verify");
        AllowAnonymous();
    }

    public override async Task<UserTaskInvitationVerificationResultWithSession> ExecuteAsync(VerifyUserTaskInvitationApiRequest request, CancellationToken cancellationToken)
    {
        var result = await invitations.VerifyAsync(new UserTaskInvitationChallenge(Route<string>("token")!, request.Code, request.State), cancellationToken);
        if (!result.Succeeded)
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, cancellationToken);
        return result;
    }
}

internal sealed class ListEventsEndpoint(IUserTaskManager manager, IUserTaskIdentityResolver identityResolver) : ElsaEndpointWithoutRequest<IReadOnlyCollection<UserTaskEvent>>
{
    public override void Configure()
    {
        Get("/user-tasks/{taskId}/events");
        ConfigurePermissions(UserTasksPermissions.Read);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        var detail = actor == null ? null : await manager.GetAsync(actor.Subject.TenantId, Route<string>("taskId")!, actor, cancellationToken);
        if (detail == null)
        {
            await Send.NotFoundAsync(cancellationToken);
            return;
        }
        await Send.OkAsync(detail.Events, cancellationToken);
    }
}

internal sealed class ListParticipantsEndpoint(IUserTaskParticipantDirectory directory, IUserTaskIdentityResolver identityResolver) : ElsaEndpoint<UserTaskParticipantLookupApiRequest, ParticipantSearchResult>
{
    public override void Configure()
    {
        Get("/user-task-participants");
        ConfigurePermissions(UserTasksPermissions.LookupParticipants);
    }

    public override async Task<ParticipantSearchResult> ExecuteAsync(UserTaskParticipantLookupApiRequest request, CancellationToken cancellationToken)
    {
        var actor = await identityResolver.ResolveAsync(User, cancellationToken);
        if (actor == null)
        {
            await Send.UnauthorizedAsync(cancellationToken);
            return new ParticipantSearchResult([], null, null);
        }

        return await directory.SearchAsync(new UserTaskParticipantQuery(actor.Subject.TenantId, request.Search, request.Type,
            request.Cursor, Math.Clamp(request.Limit <= 0 ? 50 : request.Limit, 1, 200)), cancellationToken);
    }
}
