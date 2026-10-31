using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Models;

namespace Elsa.UserTasks.Persistence.ConformanceTests.Faults;

/// <summary>
/// Thrown by the fault decorators. A distinct type so a test can tell an injected failure apart from a
/// genuine one and never accidentally assert on the wrong exception.
/// </summary>
public sealed class InjectedStoreFaultException(string operation)
    : Exception($"Injected store failure on '{operation}'.");

/// <summary>
/// Wraps a real repository and fails the first N calls to a chosen operation, so a test can assert that the
/// system converges on retry rather than committing half a change.
///
/// Every defect this suite exists to pin was found by injecting a failure, not by a happy-path test, so the
/// decorators are part of the suite's surface rather than a helper hidden in one test file.
/// </summary>
public sealed class FaultingUserTaskRepository(IUserTaskRepository inner) : IUserTaskRepository
{
    /// <summary>Number of leading <see cref="SaveAsync"/> calls that throw before the real one runs.</summary>
    public int FailSaveCalls { get; set; }

    /// <summary>Number of leading <see cref="TryMutateAsync"/> calls that throw before the real one runs.</summary>
    public int FailTryMutateCalls { get; set; }

    /// <summary>Number of leading <see cref="AppendEventAsync"/> calls that throw before the real one runs.</summary>
    public int FailAppendEventCalls { get; set; }

    public int SaveCallCount { get; private set; }
    public int TryMutateCallCount { get; private set; }
    public int AppendEventCallCount { get; private set; }

    public Task<UserTask?> GetAsync(string tenantId, string taskId, CancellationToken cancellationToken = default) =>
        inner.GetAsync(tenantId, taskId, cancellationToken);

    public Task<UserTaskQueryResult> QueryAsync(UserTaskQuery query, CancellationToken cancellationToken = default) =>
        inner.QueryAsync(query, cancellationToken);

    public Task<UserTask?> FindByMaterializationKeyAsync(string tenantId, string key, CancellationToken cancellationToken = default) =>
        inner.FindByMaterializationKeyAsync(tenantId, key, cancellationToken);

    public Task<UserTask?> FindByBookmarkIdAsync(string tenantId, string bookmarkId, CancellationToken cancellationToken = default) =>
        inner.FindByBookmarkIdAsync(tenantId, bookmarkId, cancellationToken);

    public Task<(UserTask Task, UserTaskInvitation Invitation)?> FindByInvitationTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        inner.FindByInvitationTokenHashAsync(tokenHash, cancellationToken);

    public Task AddProjectionAsync(UserTask task, CancellationToken cancellationToken = default) =>
        inner.AddProjectionAsync(task, cancellationToken);

    public Task SaveAsync(UserTask task, int expectedRevision, CancellationToken cancellationToken = default)
    {
        SaveCallCount++;
        if (FailSaveCalls-- > 0)
            throw new InjectedStoreFaultException(nameof(SaveAsync));
        return inner.SaveAsync(task, expectedRevision, cancellationToken);
    }

    public Task AppendEventAsync(string tenantId, string taskId, UserTaskEvent @event, CancellationToken cancellationToken = default)
    {
        AppendEventCallCount++;
        if (FailAppendEventCalls-- > 0)
            throw new InjectedStoreFaultException(nameof(AppendEventAsync));
        return inner.AppendEventAsync(tenantId, taskId, @event, cancellationToken);
    }

    public Task<bool> TryMutateAsync(string tenantId, string taskId, int expectedRevision, Func<UserTask, bool> mutation, CancellationToken cancellationToken = default)
    {
        TryMutateCallCount++;
        if (FailTryMutateCalls-- > 0)
            throw new InjectedStoreFaultException(nameof(TryMutateAsync));
        return inner.TryMutateAsync(tenantId, taskId, expectedRevision, mutation, cancellationToken);
    }
}

/// <summary>
/// Wraps a real guest session issuer to drive the two failure shapes that produced live-credential defects:
/// a session store that fails during revocation, and a session issued inside the revoke commit window.
/// </summary>
public sealed class FaultingGuestSessionIssuer(IUserTaskGuestSessionIssuer inner) : IUserTaskGuestSessionIssuer
{
    /// <summary>
    /// Decides, from the 1-based revocation-sweep ordinal, whether that sweep throws instead of running.
    /// Revocation deliberately sweeps on both sides of its commit, and the two sides fail differently, so a
    /// test has to be able to name which one it is breaking.
    /// </summary>
    public Func<int, bool>? FailRevokeForInvitationWhen { get; set; }

    /// <summary>Runs immediately after a session is issued, to interleave a revoke against a live verify.</summary>
    public Func<Task>? AfterIssue { get; set; }

    /// <summary>Runs before a revocation sweep, given its 1-based ordinal, to interleave against it.</summary>
    public Func<int, Task>? BeforeRevokeForInvitation { get; set; }

    public int IssueCallCount { get; private set; }
    public int RevokeForInvitationCallCount { get; private set; }
    public int RevokeForTaskCallCount { get; private set; }

    /// <summary>Zeroes the counters so a test can express its expectations relative to its own arrangement.</summary>
    public void ResetCounters() => IssueCallCount = RevokeForInvitationCallCount = RevokeForTaskCallCount = 0;

    public async Task<GuestSessionResult> IssueAsync(UserTaskInvitation invitation, ParticipantReference subject, CancellationToken cancellationToken = default)
    {
        IssueCallCount++;
        var result = await inner.IssueAsync(invitation, subject, cancellationToken);
        if (AfterIssue is { } callback)
            await callback();
        return result;
    }

    public Task<UserTaskGuestSession?> ResolveAsync(string credential, CancellationToken cancellationToken = default) =>
        inner.ResolveAsync(credential, cancellationToken);

    public Task RevokeForTaskAsync(string tenantId, string taskId, CancellationToken cancellationToken = default)
    {
        RevokeForTaskCallCount++;
        return inner.RevokeForTaskAsync(tenantId, taskId, cancellationToken);
    }

    public async Task RevokeForInvitationAsync(string tenantId, string invitationId, CancellationToken cancellationToken = default)
    {
        var ordinal = ++RevokeForInvitationCallCount;
        if (BeforeRevokeForInvitation is { } callback)
            await callback(ordinal);
        if (FailRevokeForInvitationWhen?.Invoke(ordinal) == true)
            throw new InjectedStoreFaultException(nameof(RevokeForInvitationAsync));
        await inner.RevokeForInvitationAsync(tenantId, invitationId, cancellationToken);
    }
}
