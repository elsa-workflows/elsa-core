using Elsa.Workflows.Exceptions;
using JetBrains.Annotations;
using Npgsql;

namespace Elsa.EntityFrameworkCore.PostgreSql.Handlers;

/// Transforms database exceptions encountered when using a postgreSQL database into more generic exceptions.
[UsedImplicitly]
public class DbExceptionTransformer : IDbExceptionHandler
{
    public Task HandleAsync(DbUpdateExceptionContext context)
    {
        var exception = context.Exception;
        
        if (exception.InnerException is PostgresException { SqlState: "23505" })
            throw new UniqueKeyConstraintViolationException("Unable to save data", exception);

        throw new DataProcessingException("Unable to save data", exception);
    }
}