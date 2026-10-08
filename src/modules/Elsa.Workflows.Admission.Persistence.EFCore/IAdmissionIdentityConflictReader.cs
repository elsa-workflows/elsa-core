namespace Elsa.Workflows.Admission.Persistence.EFCore;

/// <summary>
/// Optional sibling identity reservation lookup. The caller has already acquired the
/// subscription lock inside its transaction. Implementations must use that exact
/// connection and transaction, and must propagate uncertainty rather than return false.
/// This is not an execution, acknowledgement or mutation contract.
/// </summary>
public interface IAdmissionIdentityConflictReader
{
    Task<bool> HasConflictAsync(AdmissionElsaDbContext transactionContext, string identityHash, CancellationToken cancellationToken = default);
}
