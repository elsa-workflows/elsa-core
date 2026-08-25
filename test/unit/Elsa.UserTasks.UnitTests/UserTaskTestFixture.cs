using Elsa.Common;
using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Repositories;
using Elsa.UserTasks.Services;
using Elsa.Workflows;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Elsa.UserTasks.UnitTests;

/// <summary>
/// Wires the Core User Tasks slice against in-memory doubles. Every test shares this arrangement so a
/// behavior change surfaces in one place instead of being restated in each test body.
/// </summary>
public sealed class UserTaskTestFixture
{
    public const string TenantId = "tenant";

    public InMemoryUserTaskRepository Repository { get; } = new();
    public TestClock Clock { get; } = new();
    public TestIdentityGenerator Identity { get; } = new();
    public TestResumer Resumer { get; } = new();
    public TestSink Sink { get; } = new();
    public CapturingDispatcher Dispatcher { get; } = new();
    public DefaultUserTaskAccessPolicy Policy { get; } = new();
    public IOptions<UserTasksOptions> Options { get; }
    public InMemoryUserTaskGuestSessionIssuer GuestSessions { get; }
    public InMemoryUserTaskInvitationOutbox Outbox { get; }
    public DefaultUserTaskInvitationService Invitations { get; }
    public UserTaskGuestActorResolver GuestActors { get; }
    public DefaultUserTaskManager Manager { get; }
    public DefaultUserTaskProjectionService Projection { get; }

    public UserTaskTestFixture(UserTasksOptions? options = null, IUserTaskInvitationVerifier? verifier = null, params IUserTaskFormProvider[] formProviders)
    {
        Options = Microsoft.Extensions.Options.Options.Create(options ?? new UserTasksOptions());
        GuestSessions = new(Clock, Options);
        Outbox = new(new PassthroughDataProtectionProvider(), Clock, Options);
        Manager = new(Repository, Policy, formProviders, Resumer, Sink, Identity, Clock, Options);
        Projection = new(Manager, Repository, Sink, GuestSessions, Identity, Clock);
        Invitations = new(Repository, Policy, Outbox, verifier ?? new DefaultUserTaskInvitationVerifier(), GuestSessions, Sink, Identity, Clock, Options);
        GuestActors = new(GuestSessions);
    }

    public UserTaskActor Actor(string id, params string[] permissions) =>
        new(new(TenantId, "oidc", UserTaskParticipantType.User, id), [])
        {
            Permissions = new HashSet<string>(permissions.Length > 0 ? permissions : ["read:user-tasks", "claim:user-tasks", "complete:user-tasks"], StringComparer.OrdinalIgnoreCase)
        };

    public UserTaskActor ManagerActor(string id = "manager-1") => Actor(id) with
    {
        IsManager = true,
        Permissions = new HashSet<string>([
            "read:user-tasks", "claim:user-tasks", "complete:user-tasks", "assign:user-tasks",
            "update:user-tasks", "cancel:user-tasks", "invite:user-tasks", "manage:user-tasks"
        ], StringComparer.OrdinalIgnoreCase)
    };

    public UserTaskMaterialization Materialization(ParticipantReference candidate, Func<UserTaskDefinitionSnapshot, UserTaskDefinitionSnapshot>? configure = null)
    {
        var definition = new UserTaskDefinitionSnapshot
        {
            Title = "Approval",
            CandidateUsers = [candidate],
            Instructions = "private instructions",
            Actions = [new("Approve", "Approve"), new("Reject", "Reject")]
        };
        return new(TenantId, "definition", "instance", "activity", "bookmark",
            configure?.Invoke(definition) ?? definition, [], [], Clock.UtcNow, "task-1",
            "Approval workflow", 3, "correlation-1");
    }

    /// <summary>Projects a task and returns it, so tests can start from a committed projection in one line.</summary>
    public async Task<UserTask> ProjectAsync(ParticipantReference candidate, Func<UserTaskDefinitionSnapshot, UserTaskDefinitionSnapshot>? configure = null) =>
        (await Manager.ProjectAsync(Materialization(candidate, configure))).Task;

    /// <summary>Drives the guest flow end to end and returns the resolved guest actor.</summary>
    public async Task<(UserTaskActor Guest, string Credential)> IssueGuestSessionAsync(UserTask task, UserTaskActor manager, string verifierName = "bearer", params string[] actions)
    {
        var issued = await Invitations.IssueAsync(TenantId, task.Id, new(task.Revision, verifierName, actions.Length > 0 ? actions : ["Approve"]), manager);
        if (issued == null)
            throw new InvalidOperationException("The invitation could not be issued.");

        await DrainOutboxAsync();
        var verified = await Invitations.VerifyAsync(new(Dispatcher.Token!));
        if (!verified.Succeeded)
            throw new InvalidOperationException("The invitation could not be verified.");

        var guest = await GuestActors.ResolveAsync(verified.SessionToken!)
                    ?? throw new InvalidOperationException("The guest session did not resolve.");
        return (guest, verified.SessionToken!);
    }

    /// <summary>Runs the delivery step the hosted worker would normally perform.</summary>
    public async Task DrainOutboxAsync()
    {
        foreach (var delivery in await Outbox.DequeueDueAsync(50))
        {
            await Dispatcher.DispatchAsync(delivery);
            await Outbox.CompleteAsync(delivery.Id);
        }
    }

    /// <summary>
    /// Wraps a real issuer and fails a configurable number of revocation calls, so tests can drive the
    /// cross-store failure path between the invitation aggregate and the session store.
    /// </summary>
    public sealed class FaultyRevocationSessionIssuer(IUserTaskGuestSessionIssuer inner, int failures) : IUserTaskGuestSessionIssuer
    {
        private int _remaining = failures;

        public int RevokeCallCount { get; private set; }

        public Task<GuestSessionResult> IssueAsync(UserTaskInvitation invitation, ParticipantReference subject, CancellationToken cancellationToken = default) =>
            inner.IssueAsync(invitation, subject, cancellationToken);

        public Task<UserTaskGuestSession?> ResolveAsync(string credential, CancellationToken cancellationToken = default) =>
            inner.ResolveAsync(credential, cancellationToken);

        public Task RevokeForTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken = default) =>
            inner.RevokeForTaskAsync(tenantId, taskId, cancellationToken);

        public Task RevokeForInvitationAsync(string tenantId, string invitationId, CancellationToken cancellationToken = default)
        {
            RevokeCallCount++;
            if (_remaining-- > 0)
                throw new InvalidOperationException("Simulated session-store failure.");
            return inner.RevokeForInvitationAsync(tenantId, invitationId, cancellationToken);
        }
    }

    public sealed class TestClock : ISystemClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
    }

    public sealed class TestIdentityGenerator : IIdentityGenerator
    {
        private int _counter;
        public string GenerateId() => $"id-{Interlocked.Increment(ref _counter)}";
    }

    public sealed class TestResumer : IUserTaskWorkflowResumer
    {
        public UserTaskStimulus? LastStimulus { get; private set; }

        public Task ResumeAsync(UserTask task, UserTaskStimulus stimulus, CancellationToken cancellationToken = default)
        {
            LastStimulus = stimulus;
            return Task.CompletedTask;
        }
    }

    public sealed class TestSink : IUserTaskNotificationSink
    {
        public List<UserTaskLifecycleNotification> Published { get; } = [];

        public Task PublishAsync(UserTaskLifecycleNotification notification, CancellationToken cancellationToken = default)
        {
            Published.Add(notification);
            return Task.CompletedTask;
        }
    }

    public sealed class CapturingDispatcher : IUserTaskInvitationDispatcher
    {
        public string? Token { get; private set; }
        public List<string> Tokens { get; } = [];

        public Task DispatchAsync(UserTaskInvitationDelivery delivery, CancellationToken cancellationToken = default)
        {
            Token = delivery.Token;
            Tokens.Add(delivery.Token);
            return Task.CompletedTask;
        }
    }

    /// <summary>Accepts any challenge. Used to exercise the non-bearer verification path.</summary>
    public sealed class AcceptingVerifier(string? subject = null) : IUserTaskInvitationVerifier
    {
        public Task<UserTaskInvitationVerificationResult> VerifyAsync(UserTaskInvitationChallenge challenge, CancellationToken cancellationToken = default) =>
            Task.FromResult(new UserTaskInvitationVerificationResult(challenge.Code == "correct", Subject: subject));
    }

    /// <summary>
    /// Data Protection stand-in. The outbox's contract is only that the ciphertext round-trips, so the test
    /// double marks the payload rather than pulling the full key-management stack into a unit test.
    /// </summary>
    private sealed class PassthroughDataProtectionProvider : IDataProtectionProvider, IDataProtector
    {
        private const string Marker = "protected:";

        public IDataProtector CreateProtector(string purpose) => this;
        public byte[] Protect(byte[] plaintext) => System.Text.Encoding.UTF8.GetBytes(Marker + Convert.ToBase64String(plaintext));

        public byte[] Unprotect(byte[] protectedData)
        {
            var value = System.Text.Encoding.UTF8.GetString(protectedData);
            if (!value.StartsWith(Marker, StringComparison.Ordinal))
                throw new System.Security.Cryptography.CryptographicException("The payload was not protected by this provider.");
            return Convert.FromBase64String(value[Marker.Length..]);
        }
    }
}

/// <summary>A form provider that returns a fixed descriptor set, including one masked, revealable field.</summary>
public sealed class TestFormProvider(params UserTaskFormFieldDescriptor[] fields) : IUserTaskFormProvider
{
    public string Name => "test";

    public Task<ResolvedUserTaskForm?> ResolveAsync(UserTaskFormReference reference, CancellationToken cancellationToken = default) =>
        Task.FromResult<ResolvedUserTaskForm?>(new(reference, "v1", new Dictionary<string, object?>()) { Fields = fields });

    public Task<UserTaskFormValidationResult> ValidateAndNormalizeAsync(ResolvedUserTaskForm form, string actionKey, System.Text.Json.JsonElement data, CancellationToken cancellationToken = default) =>
        Task.FromResult(new UserTaskFormValidationResult(true, data));
}
