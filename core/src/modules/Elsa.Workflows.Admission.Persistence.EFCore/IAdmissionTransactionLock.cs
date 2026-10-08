namespace Elsa.Workflows.Admission.Persistence.EFCore;

/// <summary>Selected provider serialization, held until the caller's transaction ends.</summary>
public interface IAdmissionTransactionLock
{
    Task AcquireAsync(AdmissionElsaDbContext context, string subscriptionId, CancellationToken cancellationToken);
}
