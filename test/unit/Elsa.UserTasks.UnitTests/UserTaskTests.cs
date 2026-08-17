using System.Security.Claims;
using Elsa.Common;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Repositories;
using Elsa.UserTasks.Services;
using Elsa.Workflows;
using Microsoft.Extensions.Options;
using Xunit;

namespace Elsa.UserTasks.UnitTests;

public class UserTaskTests
{
    [Fact]
    public async Task ClaimsResolver_PreservesExternalGroupClaimValues()
    {
        var resolver = new DefaultClaimsIdentityResolver(Microsoft.Extensions.Options.Options.Create(new UserTasksOptions { DefaultTenantId = "tenant", DefaultProvider = "oidc" }));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "user-1"),
            new Claim("groups", "group,with;delimiters"),
            new Claim("groups", "finance"),
            new Claim("permission", "read:user-tasks")
        ], "test"));

        var actor = await resolver.ResolveAsync(principal);

        Assert.NotNull(actor);
        Assert.Contains(actor.Groups, x => x.Id == "group,with;delimiters");
        Assert.Contains(actor.Groups, x => x.Id == "finance");
        Assert.Equal("tenant", actor.Subject.TenantId);
        Assert.Equal("oidc", actor.Subject.Provider);
    }

    [Fact]
    public async Task Repository_CursorIsStableAndTotalCountIgnoresCursor()
    {
        var repository = new InMemoryUserTaskRepository();
        for (var i = 0; i < 3; i++)
            await repository.AddProjectionAsync(new UserTask { Id = $"task-{i}", TenantId = "tenant", Title = $"Task {i}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(i) });

        var first = await repository.QueryAsync(new UserTaskQuery { TenantId = "tenant", Limit = 2, IncludeTotalCount = true });
        var second = await repository.QueryAsync(new UserTaskQuery { TenantId = "tenant", Limit = 2, Cursor = first.NextCursor, IncludeTotalCount = true });

        Assert.Equal(3, first.TotalCount);
        Assert.Equal(3, second.TotalCount);
        Assert.Single(second.Items);
        Assert.DoesNotContain(first.Items.Select(x => x.Id), x => second.Items.Any(y => y.Id == x));
    }

    [Theory]
    [InlineData("created", false)]
    [InlineData("created", true)]
    [InlineData("due", false)]
    [InlineData("due", true)]
    [InlineData("priority", false)]
    [InlineData("priority", true)]
    [InlineData("title", false)]
    [InlineData("title", true)]
    public async Task Repository_CursorCoversSupportedSortsAndDirections(string sort, bool descending)
    {
        var repository = new InMemoryUserTaskRepository();
        var now = DateTimeOffset.UtcNow;
        await repository.AddProjectionAsync(new UserTask { Id = "task-a", TenantId = "tenant", Title = "Alpha", Priority = 10, DueAt = now.AddHours(1), CreatedAt = now.AddMinutes(1) });
        await repository.AddProjectionAsync(new UserTask { Id = "task-b", TenantId = "tenant", Title = "Beta", Priority = 50, DueAt = null, CreatedAt = now.AddMinutes(2) });
        await repository.AddProjectionAsync(new UserTask { Id = "task-c", TenantId = "tenant", Title = "Gamma", Priority = 90, DueAt = now.AddHours(2), CreatedAt = now.AddMinutes(3) });

        var first = await repository.QueryAsync(new UserTaskQuery { TenantId = "tenant", Limit = 2, Sort = sort, Descending = descending, IncludeTotalCount = true });
        var second = await repository.QueryAsync(new UserTaskQuery { TenantId = "tenant", Limit = 2, Sort = sort, Descending = descending, Cursor = first.NextCursor, IncludeTotalCount = true });
        var ids = first.Items.Concat(second.Items).Select(x => x.Id).ToArray();

        Assert.Equal(3, first.TotalCount);
        Assert.Equal(3, second.TotalCount);
        Assert.Equal(3, ids.Distinct().Count());
    }

    [Fact]
    public async Task Manager_HidesProtectedFieldsUntilClaimAndCompletesAfterBookmarkFinalization()
    {
        var fixture = new Fixture();
        var actor = fixture.Actor("user-1");
        var materialization = fixture.Materialization(actor.Subject);
        var projected = await fixture.Manager.ProjectAsync(materialization);

        var candidateDetail = await fixture.Manager.GetAsync("tenant", projected.Task.Id, actor);
        Assert.NotNull(candidateDetail);
        Assert.Null(candidateDetail.Instructions);
        Assert.Empty(candidateDetail.Actions);

        var claimed = await fixture.Manager.ClaimAsync("tenant", projected.Task.Id, new UserTaskMutationRequest(1, "claim-1"), actor);
        Assert.True(claimed.Accepted);
        var assignedDetail = await fixture.Manager.GetAsync("tenant", projected.Task.Id, actor);
        Assert.Equal("private instructions", assignedDetail!.Instructions);
        Assert.NotEmpty(assignedDetail.Actions);

        var completing = await fixture.Manager.CompleteAsync("tenant", projected.Task.Id,
            new UserTaskCompletionRequest(claimed.Task.Revision, "complete-1", "Approve"), actor);
        Assert.True(completing.Accepted);
        Assert.Equal(UserTaskStatus.Completing, completing.Task.Status);
        Assert.Equal(actor.Subject, fixture.Resumer.LastStimulus!.CompletedBy);

        var retry = await fixture.Manager.CompleteAsync("tenant", projected.Task.Id,
            new UserTaskCompletionRequest(claimed.Task.Revision, "complete-1", "Approve"), actor);
        Assert.True(retry.Accepted);
        Assert.Equal(completing.Operation.OperationId, retry.Operation.OperationId);

        var divergent = await fixture.Manager.CompleteAsync("tenant", projected.Task.Id,
            new UserTaskCompletionRequest(claimed.Task.Revision, "complete-1", "Different"), actor);
        Assert.False(divergent.Accepted);
        Assert.Equal("idempotency-conflict", divergent.ConflictCode);

        await fixture.Projection.FinalizeBookmarkRemovalAsync(new UserTaskBookmarkRemoval("tenant", projected.Task.Id, projected.Task.BookmarkId, fixture.Clock.UtcNow));
        var completed = await fixture.Repository.GetAsync("tenant", projected.Task.Id);
        Assert.Equal(UserTaskStatus.Completed, completed!.Status);

        var terminalRetry = await fixture.Manager.CompleteAsync("tenant", projected.Task.Id,
            new UserTaskCompletionRequest(claimed.Task.Revision, "complete-1", "Approve"), actor);
        Assert.True(terminalRetry.Accepted);
        Assert.Equal(completing.Operation.OperationId, terminalRetry.Operation.OperationId);
    }

    [Fact]
    public async Task PolicyRequiresOperationPermissionInAdditionToCandidateRelationship()
    {
        var fixture = new Fixture();
        var actor = fixture.Actor("user-1") with
        {
            Permissions = new HashSet<string>(["read:user-tasks"], StringComparer.OrdinalIgnoreCase)
        };
        var projected = await fixture.Manager.ProjectAsync(fixture.Materialization(actor.Subject));

        var claim = await fixture.Manager.ClaimAsync("tenant", projected.Task.Id, new UserTaskMutationRequest(1, "claim-no-permission"), actor);

        Assert.False(claim.Accepted);
        Assert.Equal("forbidden", claim.ConflictCode);
    }

    [Fact]
    public async Task DueServiceReservesTimeoutAndResumesWorkflow()
    {
        var fixture = new Fixture();
        var dueAt = fixture.Clock.UtcNow.AddMinutes(-1);
        var baseMaterialization = fixture.Materialization(fixture.Actor("user-1").Subject);
        var materialization = baseMaterialization with
        {
            Definition = baseMaterialization.Definition with
            {
                DueAt = dueAt,
                EnableTimeoutOutcome = true
            }
        };
        var projected = await fixture.Manager.ProjectAsync(materialization);
        var due = new DefaultUserTaskDueService(fixture.Repository, fixture.Manager, fixture.Sink, fixture.Identity, fixture.Clock);

        Assert.Equal(1, await due.MarkOverdueAsync("tenant", fixture.Clock.UtcNow));
        var timingOut = await fixture.Repository.GetAsync("tenant", projected.Task.Id);
        Assert.Equal(UserTaskStatus.TimingOut, timingOut!.Status);
        Assert.Equal("Timeout", fixture.Resumer.LastStimulus!.ActionKey);

        await fixture.Projection.FinalizeBookmarkRemovalAsync(new UserTaskBookmarkRemoval("tenant", projected.Task.Id, projected.Task.BookmarkId, fixture.Clock.UtcNow));
        var timedOut = await fixture.Repository.GetAsync("tenant", projected.Task.Id);
        Assert.Equal(UserTaskStatus.TimedOut, timedOut!.Status);
    }

    [Fact]
    public async Task InvitationUsesOneTimeSecretAndRevokesAfterGuestClaim()
    {
        var fixture = new Fixture();
        var manager = fixture.ManagerActor("manager-1");
        var baseMaterialization = fixture.Materialization(fixture.Actor("user-1").Subject);
        var materialization = baseMaterialization with
        {
            Definition = baseMaterialization.Definition with
            {
                Invitations = [new UserTaskInvitationDefinition("bearer", ["Approve"], BearerOnly: true)]
            }
        };
        var projected = await fixture.Manager.ProjectAsync(materialization);
        var dispatcher = new CapturingDispatcher();
        var invitations = new DefaultUserTaskInvitationService(
            fixture.Repository,
            new DefaultUserTaskAccessPolicy(),
            dispatcher,
            new DefaultUserTaskInvitationVerifier(),
            new DefaultUserTaskGuestSessionIssuer(fixture.Clock),
            fixture.Sink,
            fixture.Identity,
            fixture.Clock,
            Microsoft.Extensions.Options.Options.Create(new UserTasksOptions()));

        var issued = await invitations.IssueAsync("tenant", projected.Task.Id,
            new UserTaskInvitationIssueRequest(1, "bearer", ["Approve"]), manager);
        Assert.NotNull(issued);
        Assert.NotNull(dispatcher.Token);
        var firstToken = dispatcher.Token!;
        Assert.DoesNotContain(firstToken, System.Text.Json.JsonSerializer.Serialize(issued));

        var secondIssued = await invitations.IssueAsync("tenant", projected.Task.Id,
            new UserTaskInvitationIssueRequest(2, "bearer", ["Approve"]), manager);
        Assert.NotNull(secondIssued);
        var secondToken = dispatcher.Token!;

        var verified = await invitations.VerifyAsync(new UserTaskInvitationChallenge(firstToken));
        Assert.True(verified.Succeeded);
        Assert.Equal(projected.Task.Id, verified.TaskId);
        Assert.NotNull(verified.SessionToken);

        var sibling = await invitations.VerifyAsync(new UserTaskInvitationChallenge(secondToken));
        Assert.False(sibling.Succeeded);
        Assert.Equal("invitation-unavailable", sibling.FailureCode);
        var replay = await invitations.VerifyAsync(new UserTaskInvitationChallenge(firstToken));
        Assert.False(replay.Succeeded);
        Assert.Equal("invitation-unavailable", replay.FailureCode);
        var claimed = await fixture.Repository.GetAsync("tenant", projected.Task.Id);
        Assert.Equal(UserTaskStatus.Assigned, claimed!.Status);
        Assert.Equal("guest", claimed.Assignee!.Provider);
    }

    private sealed class Fixture
    {
        public readonly InMemoryUserTaskRepository Repository = new();
        public readonly TestClock Clock = new();
        public readonly TestIdentityGenerator Identity = new();
        public readonly TestResumer Resumer = new();
        public readonly TestSink Sink = new();
        public readonly DefaultUserTaskManager Manager;
        public readonly DefaultUserTaskProjectionService Projection;

        public Fixture()
        {
            Manager = new DefaultUserTaskManager(Repository, new DefaultUserTaskAccessPolicy(), [], Resumer, Sink, Identity, Clock, Microsoft.Extensions.Options.Options.Create(new UserTasksOptions()));
            Projection = new DefaultUserTaskProjectionService(Manager, Repository, Sink, Identity, Clock);
        }

        public UserTaskActor Actor(string id) => new(new ParticipantReference("tenant", "oidc", UserTaskParticipantType.User, id), [])
        {
            Permissions = new HashSet<string>(["read:user-tasks", "claim:user-tasks", "complete:user-tasks"], StringComparer.OrdinalIgnoreCase)
        };

        public UserTaskActor ManagerActor(string id) => Actor(id) with
        {
            IsManager = true,
            Permissions = new HashSet<string>(["read:user-tasks", "invite:user-tasks", "manage:user-tasks"], StringComparer.OrdinalIgnoreCase)
        };

        public UserTaskMaterialization Materialization(ParticipantReference candidate) => new("tenant", "definition", "instance", "activity", "bookmark",
            new UserTaskDefinitionSnapshot
            {
                Title = "Approval",
                CandidateUsers = [candidate],
                Instructions = "private instructions",
                Actions = [new UserTaskAction("Approve", "Approve")]
            }, [], [], Clock.UtcNow, "task-1");
    }

    private sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    }

    private sealed class TestIdentityGenerator : IIdentityGenerator
    {
        private int _counter;
        public string GenerateId() => $"id-{Interlocked.Increment(ref _counter)}";
    }

    private sealed class TestResumer : IUserTaskWorkflowResumer
    {
        public UserTaskStimulus? LastStimulus { get; private set; }
        public Task ResumeAsync(UserTask task, UserTaskStimulus stimulus, CancellationToken cancellationToken = default)
        {
            LastStimulus = stimulus;
            return Task.CompletedTask;
        }
    }

    private sealed class TestSink : IUserTaskNotificationSink
    {
        public Task PublishAsync(UserTaskLifecycleNotification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CapturingDispatcher : IUserTaskInvitationDispatcher
    {
        public string? Token { get; private set; }

        public Task DispatchAsync(UserTaskInvitationDelivery delivery, CancellationToken cancellationToken = default)
        {
            Token = delivery.Token;
            return Task.CompletedTask;
        }
    }
}
