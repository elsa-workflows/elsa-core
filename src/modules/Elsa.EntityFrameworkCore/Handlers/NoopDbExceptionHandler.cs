using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Handlers;

/// A No-Op database exception handler.
public class NoopDbExceptionHandler : IDbExceptionHandler<AlterationsElsaDbContext>,
    IDbExceptionHandler<IdentityElsaDbContext>,
    IDbExceptionHandler<LabelsElsaDbContext>,
    IDbExceptionHandler<ManagementElsaDbContext>,
    IDbExceptionHandler<RuntimeElsaDbContext>
{
    /// Handles the given exception that occurs during database operations.
    public void Handle(DbUpdateException exception)
    {
        throw exception;
    }
}