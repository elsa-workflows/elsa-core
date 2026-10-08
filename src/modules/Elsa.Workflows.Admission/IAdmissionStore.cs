namespace Elsa.Workflows.Admission;

/// <summary>
/// Transactional durable admission boundary. All mutating methods return only after definite commit;
/// an uncertain commit throws and must never be converted into authorization by readback/retry.
/// Implementations use revision predicates and transactionally update capacity alongside state.
/// </summary>
public interface IAdmissionStore
{
    /// <summary>Insert inactive configuration or verify an exact match; never overwrite/retire-reset an existing identity.</summary>
    Task<AdmissionSubscription> ProvisionAsync(AdmissionSubscriptionConfiguration configuration, CancellationToken cancellationToken = default);
    /// <summary>Inactive CAS edits preserve stable tenant/environment/installation/subscription identity and activation boundary; invalidate bootstrap verification.</summary>
    Task<AdmissionSubscription?> ReconfigureAsync(AdmissionSubscriptionConfiguration configuration, long revision, CancellationToken cancellationToken = default);
    Task<AdmissionSubscription?> FindSubscriptionAsync(string subscriptionId, CancellationToken cancellationToken = default);
    /// <summary>CAS verifies bootstrap, then separately activates only verified, non-retired configuration.</summary>
    Task<AdmissionSubscription?> VerifyBootstrapAsync(string subscriptionId, long revision, string configurationFingerprint, CancellationToken cancellationToken = default);
    Task<AdmissionSubscription?> ActivateAsync(string subscriptionId, long revision, CancellationToken cancellationToken = default);
    /// <summary>Withdraw first. Retirement is irreversible; failed later external mutations retain withdrawal and reconciliation.</summary>
    Task<AdmissionSubscription?> WithdrawAsync(string subscriptionId, long revision, bool retire, string? reconciliationCode, CancellationToken cancellationToken = default);
    /// <summary>Checks trusted configuration, filters, event time, unique identity and both capacity counters in one transaction.</summary>
    Task<AdmissionResult> AdmitAsync(AdmissionEvent message, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<AdmissionRecord?> FindAsync(string admissionId, CancellationToken cancellationToken = default);
    /// <summary>Includes records whose dedup identity was cleaned. Null must mean definitively unowned.</summary>
    Task<AdmissionRecord?> FindByInstanceAsync(string instanceId, CancellationToken cancellationToken = default);
    /// <summary>Bounded indexed recovery scan; never grants execution authority.</summary>
    Task<IReadOnlyList<AdmissionRecord>> FindRecoverableAsync(int limit, CancellationToken cancellationToken = default);
    /// <summary>CAS Admitted→Creating, allocating instance identity before insert-only workflow creation. Requires active binding.</summary>
    Task<AdmissionRecord?> BeginCreationAsync(string admissionId, long revision, string instanceId, CancellationToken cancellationToken = default);
    /// <summary>CAS Creating→Materialized only after definite insertion AND notification completion.</summary>
    Task<AdmissionRecord?> CompleteCreationAsync(string admissionId, long revision, CancellationToken cancellationToken = default);
    /// <summary>
    /// CAS Materialized/ExecutionObserved→StartPreparing. Continuation requires exact checkpoint and unconsumed bookmark;
    /// initial entry requires null checkpoint/bookmark. Requires active binding and no outstanding authority.
    /// </summary>
    Task<AdmissionRecord?> PrepareStartAsync(string admissionId, long revision, string attemptId, string? checkpointFingerprint, string? bookmarkId, CancellationToken cancellationToken = default);
    /// <summary>
    /// CAS StartPreparing→StartAuthorized, atomically recording outstanding authority and consuming continuation lineage.
    /// Requires active binding. Only this first definite commit may produce a process-local capability in its caller.
    /// </summary>
    Task<AdmissionRecord?> AuthorizeStartAsync(string admissionId, long revision, string attemptId, CancellationToken cancellationToken = default);
    /// <summary>
    /// CAS StartAuthorized→ExecutionObserved after all final writes/unwind. Clears outstanding authority and records
    /// trusted full-state fingerprint/bookmarks. A durably Finished workflow qualifies Completed atomically.
    /// </summary>
    Task<AdmissionRecord?> CompleteExecutionAsync(string admissionId, long revision, string attemptId, string checkpointFingerprint,
        IReadOnlyCollection<string> bookmarkIds, bool durablyCompleted, DateTimeOffset now, CancellationToken cancellationToken = default);
    /// <summary>CAS genuine uncertainty to RecoveryRequired. Does not clear outstanding authority or release capacity.</summary>
    Task<AdmissionRecord?> RequireRecoveryAsync(string admissionId, long revision, string recoveryCode, CancellationToken cancellationToken = default);
    /// <summary>
    /// Explicit audited terminal resolution. SuppressedBeforeStart requires no issued authority or unknown creation;
    /// Resolved requires established quiescence of ALL delayed owners/capabilities and no unknown effects.
    /// This method is never an automatic lease-expiry recovery operation.
    /// </summary>
    Task<AdmissionRecord?> ResolveAsync(string admissionId, long revision, AdmissionTerminalDisposition disposition,
        string auditReference, bool establishedOwnerQuiescence, bool noUnknownEffects, DateTimeOffset now, CancellationToken cancellationToken = default);
    /// <summary>
    /// Revision-checked cleanup of truly terminal records using TerminalAt clock and configured authority.
    /// Erases payload at its retention and identity at its horizon. Owned tombstones stay charged against retained-record capacity; only records without an allocated instance may be deleted and release retained capacity, exactly once.
    /// Instance ownership must survive identity erasure.
    /// </summary>
    Task<bool> CleanupAsync(string admissionId, long revision, string cleanupAuthority, DateTimeOffset now, CancellationToken cancellationToken = default);
}

/// <summary>Selected EF bootstrap insert seam. Publication/indexing remains a separate audited boundary.</summary>
public interface IAdmissionDefinitionBootstrapStore
{
    /// <summary>Selected-provider session lock covering logical definition and trusted subscription, including cross-subscription definition contention.</summary>
    Task<IAsyncDisposable> AcquireExclusiveAsync(AdmissionSubscriptionConfiguration configuration, CancellationToken cancellationToken = default);
    /// <summary>Insert fixed definition atomically if absent or verify exact tenant/id/version/content; never upsert.</summary>
    Task<AdmissionDefinitionInsertOutcome> InsertOrVerifyAsync(Elsa.Workflows.Management.Entities.WorkflowDefinition definition, string contentFingerprint, CancellationToken cancellationToken = default);
}

/// <summary>Definite insert outcome; ExistingMatch alone never permits publication or saved-notification replay.</summary>
public enum AdmissionDefinitionInsertOutcome { Inserted, ExistingMatch }
