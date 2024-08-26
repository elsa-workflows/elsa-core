using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Handlers;

/// A database exception handler that rethrows the original exception.
public class RethrowDbExceptionHandler : IDbExceptionHandler<AlterationsElsaDbContext>,
    IDbExceptionHandler<IdentityElsaDbContext>,
    IDbExceptionHandler<LabelsElsaDbContext>,
    IDbExceptionHandler<ManagementElsaDbContext>,
    IDbExceptionHandler<RuntimeElsaDbContext>
{
    /// rethrows the given exception that occurs during database operations.
    public void Handle(DbUpdateException exception)
    {
        throw exception;
    }
}