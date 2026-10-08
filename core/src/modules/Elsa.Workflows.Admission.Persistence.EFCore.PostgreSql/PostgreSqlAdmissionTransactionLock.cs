using Microsoft.EntityFrameworkCore;

namespace Elsa.Workflows.Admission.Persistence.EFCore.PostgreSql;

/// <summary>Transaction-scoped lock, including the absent-row provision/admission race.</summary>
public sealed class PostgreSqlAdmissionTransactionLock : IAdmissionTransactionLock
{
    public async Task AcquireAsync(AdmissionElsaDbContext context, string subscriptionId, CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction == null)
        {
            throw new InvalidOperationException("admission_transaction_required");
        }
        // PostgreSQL transaction locks are released on commit/rollback, including process death.
        // https://www.postgresql.org/docs/current/explicit-locking.html#ADVISORY-LOCKS
        var key = "elsa:admission:subscription:" + subscriptionId;
        await context.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({key}, 0))", cancellationToken);
    }
}
