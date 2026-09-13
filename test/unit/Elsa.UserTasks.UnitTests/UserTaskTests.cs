using System.Security.Claims;
using System.Text.Json;
using Elsa.Authorization;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Permissions;
using Elsa.UserTasks.Repositories;
using Elsa.UserTasks.Services;

namespace Elsa.UserTasks.UnitTests;

public class UserTaskTests
{
    private readonly UserTaskTestFixture _fixture = new();

    [Test]
    public async Task ClaimsResolver_PreservesExternalGroupClaimValues()
    {
        var resolver = new DefaultClaimsIdentityResolver(Microsoft.Extensions.Options.Options.Create(new UserTasksOptions { DefaultTenantId = "tenant", DefaultProvider = "oidc" }));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "user-1"),
            new Claim("groups", "group,with;delimiters"),
            new Claim("groups", "finance"),
            new Claim("permission", UserTaskTestFixture.Grant(CoreVerbs.View))
        ], "test"));

        var actor = await Assert.That(await resolver.ResolveAsync(principal)).IsNotNull();

        await Assert.That(actor.Groups).Contains(x => x.Id == "group,with;delimiters");
        await Assert.That(actor.Groups).Contains(x => x.Id == "finance");
        await Assert.That(actor.Subject.TenantId).IsEqualTo("tenant");
        await Assert.That(actor.Subject.Provider).IsEqualTo("oidc");
    }

    [Test]
    public async Task Repository_CursorIsStableAndTotalCountIgnoresCursor()
    {
        var repository = new InMemoryUserTaskRepository();
        for (var i = 0; i < 3; i++)
            await repository.AddProjectionAsync(new() { Id = $"task-{i}", TenantId = "tenant", Title = $"Task {i}", CreatedAt = DateTimeOffset.UtcNow.AddMinutes(i) });

        var first = await repository.QueryAsync(new() { TenantId = "tenant", Limit = 2, IncludeTotalCount = true });
        var second = await repository.QueryAsync(new() { TenantId = "tenant", Limit = 2, Cursor = first.NextCursor, IncludeTotalCount = true });

        await Assert.That(first.TotalCount).IsEqualTo(3);
        await Assert.That(second.TotalCount).IsEqualTo(3);
        await Assert.That(second.Items).HasSingleItem();
        await Assert.That(first.Items.Select(x => x.Id)).DoesNotContain(x => second.Items.Any(y => y.Id == x));
    }

    [Test]
    [Arguments("created", false)]
    [Arguments("created", true)]
    [Arguments("due", false)]
    [Arguments("due", true)]
    [Arguments("priority", false)]
    [Arguments("priority", true)]
    [Arguments("title", false)]
    [Arguments("title", true)]
    public async Task Repository_CursorCoversSupportedSortsAndDirections(string sort, bool descending)
    {
        var repository = new InMemoryUserTaskRepository();
        var now = DateTimeOffset.UtcNow;
        await repository.AddProjectionAsync(new() { Id = "task-a", TenantId = "tenant", Title = "Alpha", Priority = 10, DueAt = now.AddHours(1), CreatedAt = now.AddMinutes(1) });
        await repository.AddProjectionAsync(new() { Id = "task-b", TenantId = "tenant", Title = "Beta", Priority = 50, DueAt = null, CreatedAt = now.AddMinutes(2) });
        await repository.AddProjectionAsync(new() { Id = "task-c", TenantId = "tenant", Title = "Gamma", Priority = 90, DueAt = now.AddHours(2), CreatedAt = now.AddMinutes(3) });

        var first = await repository.QueryAsync(new() { TenantId = "tenant", Limit = 2, Sort = sort, Descending = descending, IncludeTotalCount = true });
        var second = await repository.QueryAsync(new() { TenantId = "tenant", Limit = 2, Sort = sort, Descending = descending, Cursor = first.NextCursor, IncludeTotalCount = true });
        var ids = first.Items.Concat(second.Items).Select(x => x.Id).ToArray();

        await Assert.That(first.TotalCount).IsEqualTo(3);
        await Assert.That(second.TotalCount).IsEqualTo(3);
        await Assert.That(ids.Distinct().Count()).IsEqualTo(3);
    }

    [Test]
    public async Task Manager_HidesProtectedFieldsUntilClaimAndCompletesAfterBookmarkFinalization()
    {
        var actor = _fixture.Actor("user-1");
        var task = await _fixture.ProjectAsync(actor.Subject);

        var candidateDetail = await Assert.That(await _fixture.Manager.GetAsync(UserTaskTestFixture.TenantId, task.Id, actor)).IsNotNull();
        await Assert.That(candidateDetail.Instructions).IsNull();
        await Assert.That(candidateDetail.Disclosure.CanViewProtected).IsFalse();

        var claimed = await _fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(1, "claim-1"), actor);
        await Assert.That(claimed.Accepted).IsTrue();
        var assignedDetail = await _fixture.Manager.GetAsync(UserTaskTestFixture.TenantId, task.Id, actor);
        await Assert.That(assignedDetail!.Instructions).IsEqualTo("private instructions");
        await Assert.That(assignedDetail.Disclosure.CanViewProtected).IsTrue();
        await Assert.That(assignedDetail.Actions).IsNotEmpty();

        var completing = await _fixture.Manager.CompleteAsync(UserTaskTestFixture.TenantId, task.Id, new(claimed.Task.Revision, "complete-1", "Approve"), actor);
        await Assert.That(completing.Accepted).IsTrue();
        await Assert.That(completing.Task.Status).IsEqualTo(UserTaskStatus.Completing);
        await Assert.That(_fixture.Resumer.LastStimulus!.CompletedBy).IsEqualTo(actor.Subject);

        var retry = await _fixture.Manager.CompleteAsync(UserTaskTestFixture.TenantId, task.Id, new(claimed.Task.Revision, "complete-1", "Approve"), actor);
        await Assert.That(retry.Accepted).IsTrue();
        await Assert.That(retry.Operation.OperationId).IsEqualTo(completing.Operation.OperationId);

        var divergent = await _fixture.Manager.CompleteAsync(UserTaskTestFixture.TenantId, task.Id, new(claimed.Task.Revision, "complete-1", "Reject"), actor);
        await Assert.That(divergent.Accepted).IsFalse();
        await Assert.That(divergent.ConflictCode).IsEqualTo("idempotency-conflict");

        await _fixture.Projection.FinalizeBookmarkRemovalAsync(new(UserTaskTestFixture.TenantId, task.Id, task.BookmarkId, _fixture.Clock.UtcNow));
        var completed = await _fixture.Repository.GetAsync(UserTaskTestFixture.TenantId, task.Id);
        await Assert.That(completed!.Status).IsEqualTo(UserTaskStatus.Completed);

        var terminalRetry = await _fixture.Manager.CompleteAsync(UserTaskTestFixture.TenantId, task.Id, new(claimed.Task.Revision, "complete-1", "Approve"), actor);
        await Assert.That(terminalRetry.Accepted).IsTrue();
        await Assert.That(terminalRetry.Operation.OperationId).IsEqualTo(completing.Operation.OperationId);
    }

    [Test]
    public async Task Policy_RequiresOperationPermissionInAdditionToCandidateRelationship()
    {
        var actor = _fixture.Actor("user-1", UserTaskTestFixture.Grant(CoreVerbs.View));
        var task = await _fixture.ProjectAsync(actor.Subject);

        var claim = await _fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(1, "claim-no-permission"), actor);

        await Assert.That(claim.Accepted).IsFalse();
        await Assert.That(claim.ConflictCode).IsEqualTo("forbidden");
    }

    [Test]
    public async Task Policy_ManagerFlagAloneDoesNotGrantManagementWithoutThePermission()
    {
        var actor = _fixture.Actor("user-1", UserTaskTestFixture.Grant(CoreVerbs.View), UserTaskTestFixture.Grant(UserTaskVerbs.Assign)) with { IsManager = true };
        var candidate = _fixture.Actor("user-2");
        var task = await _fixture.ProjectAsync(candidate.Subject);

        // The host set IsManager but never granted user-tasks:supervise, so management must still be refused.
        await Assert.That(await _fixture.Policy.AuthorizeAsync(task, actor, UserTaskAccessOperation.Assign)).IsFalse();
        await Assert.That(await _fixture.Policy.CreateScopeAsync(actor, UserTaskQueryScopeKind.All)).IsNull();
    }

    [Test]
    public async Task Policy_WildcardPermissionGrantSatisfiesManagementChecks()
    {
        var root = _fixture.Actor("root", "*") with { IsManager = true };
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject);

        await Assert.That(await _fixture.Policy.AuthorizeAsync(task, root, UserTaskAccessOperation.Manage)).IsTrue();
        var scope = await Assert.That(await _fixture.Policy.CreateScopeAsync(root, UserTaskQueryScopeKind.All)).IsNotNull();
        await Assert.That(scope.IsManager).IsTrue();
    }

    [Test]
    [Arguments(UserTaskQueryScopeKind.All)]
    [Arguments(UserTaskQueryScopeKind.NeedsAttention)]
    public async Task Query_ManagerOnlyScopesAreDeniedRatherThanSilentlyNarrowed(UserTaskQueryScopeKind kind)
    {
        var actor = _fixture.Actor("user-1");
        await _fixture.ProjectAsync(actor.Subject);

        // A denial must be distinguishable from an empty page, or the endpoint would answer 200 with no rows.
        await Assert.That(await _fixture.Manager.QueryAsync(new() { TenantId = UserTaskTestFixture.TenantId }, kind, actor)).IsNull();
    }

    [Test]
    public async Task Query_AvailableScopeExcludesClaimedTasksAndExcludedCandidates()
    {
        var candidate = _fixture.Actor("user-1");
        var excluded = _fixture.Actor("user-2");
        var open = await _fixture.ProjectAsync(candidate.Subject, definition => definition with
        {
            CandidateUsers = [candidate.Subject, excluded.Subject],
            ExcludedUsers = [excluded.Subject]
        });

        var availableToCandidate = await Assert.That(
            await _fixture.Manager.QueryAsync(new() { TenantId = UserTaskTestFixture.TenantId }, UserTaskQueryScopeKind.Available, candidate)).IsNotNull();
        await Assert.That(availableToCandidate.Items).HasSingleItem();

        var availableToExcluded = await Assert.That(
            await _fixture.Manager.QueryAsync(new() { TenantId = UserTaskTestFixture.TenantId }, UserTaskQueryScopeKind.Available, excluded)).IsNotNull();
        await Assert.That(availableToExcluded.Items).IsEmpty();

        await _fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, open.Id, new(1, "claim-1"), candidate);

        var afterClaim = await Assert.That(
            await _fixture.Manager.QueryAsync(new() { TenantId = UserTaskTestFixture.TenantId }, UserTaskQueryScopeKind.Available, candidate)).IsNotNull();
        await Assert.That(afterClaim.Items).IsEmpty();

        var assigned = await Assert.That(
            await _fixture.Manager.QueryAsync(new() { TenantId = UserTaskTestFixture.TenantId }, UserTaskQueryScopeKind.Assigned, candidate)).IsNotNull();
        await Assert.That(assigned.Items).HasSingleItem();
    }

    [Test]
    public async Task Summary_DoesNotDiscloseCandidateIdentitiesOrBlockingHealthToParticipants()
    {
        var candidate = _fixture.Actor("user-1");
        var peer = _fixture.Actor("user-2");
        var task = await _fixture.ProjectAsync(candidate.Subject, definition => definition with { CandidateUsers = [candidate.Subject, peer.Subject] });
        task.HealthSeverity = UserTaskHealthSeverity.Advisory;
        task.HealthCode = "advisory-code";

        var summary = await UserTaskModelMapper.ToSummaryAsync(task, candidate, _fixture.Policy);

        await Assert.That(summary.CandidateSummary).IsEqualTo("2 users");
        await Assert.That(JsonSerializer.Serialize(summary)).DoesNotContain(peer.Subject.Id).WithComparison(StringComparison.CurrentCulture);
        await Assert.That(summary.HealthSeverity).IsNull();
        await Assert.That(summary.HealthCode).IsNull();
    }

    [Test]
    public async Task Summary_CarriesWorkflowContextForAuthorizedParticipants()
    {
        var actor = _fixture.Actor("user-1");
        var task = await _fixture.ProjectAsync(actor.Subject);

        var summary = await UserTaskModelMapper.ToSummaryAsync(task, actor, _fixture.Policy);

        await Assert.That(summary.WorkflowDefinitionName).IsEqualTo("Approval workflow");
        await Assert.That(summary.WorkflowDefinitionVersion).IsEqualTo(3);
        await Assert.That(summary.WorkflowInstanceReference).IsEqualTo("correlation-1");
    }

    [Test]
    public async Task Events_AreWithheldFromParticipantsWithoutProtectedAccess()
    {
        var candidate = _fixture.Actor("user-1");
        var task = await _fixture.ProjectAsync(candidate.Subject);

        var beforeClaim = await Assert.That(
            await _fixture.Manager.GetEventsAsync(UserTaskTestFixture.TenantId, task.Id, null, 50, candidate)).IsNotNull();
        await Assert.That(beforeClaim.Items).IsEmpty();

        await _fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(1, "claim-1"), candidate);

        var afterClaim = await Assert.That(
            await _fixture.Manager.GetEventsAsync(UserTaskTestFixture.TenantId, task.Id, null, 50, candidate)).IsNotNull();
        await Assert.That(afterClaim.Items).IsNotEmpty();
        // Actor identifiers never reach the audit projection; only a display name may.
        await Assert.That(JsonSerializer.Serialize(afterClaim)).DoesNotContain(candidate.Subject.Id).WithComparison(StringComparison.CurrentCulture);
    }

    [Test]
    public async Task Detail_WithholdsMaskedFieldValuesUntilAnExplicitRevealAndDoesNotConsumeTheRevision()
    {
        var form = new TestFormProvider(
            new("note", "Note"),
            new("iban", "IBAN", Masked: true, CanReveal: true),
            new("pin", "PIN", Masked: true));
        var fixture = new UserTaskTestFixture(formProviders: form);
        var actor = fixture.Actor("user-1");
        var payload = JsonDocument.Parse("""{"note":"visible","iban":"NL00BANK","pin":"1234"}""").RootElement;
        var task = await fixture.ProjectAsync(actor.Subject, definition => definition with
        {
            FormReference = new("test", "invoice"),
            TaskData = payload
        });
        await fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(1, "claim-1"), actor);

        var detail = await fixture.Manager.GetAsync(UserTaskTestFixture.TenantId, task.Id, actor);
        var fields = detail!.Form!.Fields.ToDictionary(x => x.Key);
        await Assert.That(fields["note"].Value?.GetString()).IsEqualTo("visible");
        await Assert.That(fields["iban"].Value).IsNull();
        await Assert.That(fields["pin"].Value).IsNull();
        await Assert.That(fields["iban"].CanReveal).IsTrue();
        await Assert.That(fields["pin"].CanReveal).IsFalse();
        await Assert.That(JsonSerializer.Serialize(detail.Form)).DoesNotContain("NL00BANK").WithComparison(StringComparison.CurrentCulture);

        var revisionBeforeReveal = detail.Revision;
        var revealed = await fixture.Manager.RevealFieldAsync(UserTaskTestFixture.TenantId, task.Id, "iban", actor);
        await Assert.That(revealed?.GetString()).IsEqualTo("NL00BANK");

        // A field the provider did not mark revealable is indistinguishable from an unknown one.
        await Assert.That(await fixture.Manager.RevealFieldAsync(UserTaskTestFixture.TenantId, task.Id, "pin", actor)).IsNull();
        await Assert.That(await fixture.Manager.RevealFieldAsync(UserTaskTestFixture.TenantId, task.Id, "nope", actor)).IsNull();

        var afterReveal = await fixture.Repository.GetAsync(UserTaskTestFixture.TenantId, task.Id);
        // The reveal is audited but must not consume the concurrency token, or the caller's next command
        // would conflict for no reason.
        await Assert.That(afterReveal!.Revision).IsEqualTo(revisionBeforeReveal);
        await Assert.That(afterReveal.Events).Contains(x => x.EventType == "FieldRevealed");
        await Assert.That(JsonSerializer.Serialize(afterReveal.Events)).DoesNotContain("NL00BANK").WithComparison(StringComparison.CurrentCulture);

        var completion = await fixture.Manager.CompleteAsync(UserTaskTestFixture.TenantId, task.Id,
            new(revisionBeforeReveal, "complete-1", "Approve", payload), actor);
        await Assert.That(completion.Accepted).IsTrue();
    }

    [Test]
    public async Task RevealField_IsRefusedForCallersWithoutProtectedAccess()
    {
        var form = new TestFormProvider(new UserTaskFormFieldDescriptor("iban", "IBAN", Masked: true, CanReveal: true));
        var fixture = new UserTaskTestFixture(formProviders: form);
        var candidate = fixture.Actor("user-1");
        var task = await fixture.ProjectAsync(candidate.Subject, definition => definition with
        {
            FormReference = new("test", "invoice"),
            TaskData = JsonDocument.Parse("""{"iban":"NL00BANK"}""").RootElement
        });

        // The candidate can see the task but has not claimed it, so protected access is not granted yet.
        await Assert.That(await fixture.Manager.RevealFieldAsync(UserTaskTestFixture.TenantId, task.Id, "iban", candidate)).IsNull();

        await fixture.Manager.ClaimAsync(UserTaskTestFixture.TenantId, task.Id, new(1, "claim-1"), candidate);
        await Assert.That(await fixture.Manager.RevealFieldAsync(UserTaskTestFixture.TenantId, task.Id, "iban", candidate)).IsNotNull();
    }

    [Test]
    public async Task DueService_ReservesTimeoutAndResumesWorkflow()
    {
        var dueAt = _fixture.Clock.UtcNow.AddMinutes(-1);
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject,
            definition => definition with { DueAt = dueAt, EnableTimeoutOutcome = true });
        var due = new DefaultUserTaskDueService(_fixture.Repository, _fixture.Manager, _fixture.Sink, _fixture.Identity, _fixture.Clock);

        await Assert.That(await due.MarkOverdueAsync(UserTaskTestFixture.TenantId, _fixture.Clock.UtcNow)).IsEqualTo(1);
        var timingOut = await _fixture.Repository.GetAsync(UserTaskTestFixture.TenantId, task.Id);
        await Assert.That(timingOut!.Status).IsEqualTo(UserTaskStatus.TimingOut);
        await Assert.That(_fixture.Resumer.LastStimulus!.ActionKey).IsEqualTo("Timeout");

        await _fixture.Projection.FinalizeBookmarkRemovalAsync(new(UserTaskTestFixture.TenantId, task.Id, task.BookmarkId, _fixture.Clock.UtcNow));
        var timedOut = await _fixture.Repository.GetAsync(UserTaskTestFixture.TenantId, task.Id);
        await Assert.That(timedOut!.Status).IsEqualTo(UserTaskStatus.TimedOut);
    }
}
