using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.EntityFrameworkCore.Modules.Alterations;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Workflows.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Elsa.EntityFrameworkCore.PostgreSql.Handlers;

/// Transforms database exceptions encountered when using a postgreSQL database into more generic exceptions.
public class DbExceptionTransformer : IDbExceptionHandler<AlterationsElsaDbContext>,
    IDbExceptionHandler<IdentityElsaDbContext>,
    IDbExceptionHandler<LabelsElsaDbContext>,
    IDbExceptionHandler<ManagementElsaDbContext>,
    IDbExceptionHandler<RuntimeElsaDbContext>
{
    /// Transforms database exceptions encountered when using a postgreSQL database into more generic exceptions.
    public void Handle(DbUpdateException exception)
    {
        if (exception.InnerException is PostgresException { SqlState: "23505" })
            throw new UniqueKeyConstraintViolationException("Unable to save data", exception);

        throw new DataProcessingException("Unable to save data", exception);
    }
}