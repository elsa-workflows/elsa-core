using Elsa.EntityFrameworkCore.Common;
using Elsa.EntityFrameworkCore.Common.Contracts;
using Elsa.Workflows.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Elsa.EntityFrameworkCore.PostgreSql.Handlers;

/// Transforms database exceptions encountered when using a postgreSQL database into more generic exceptions.
public class DataProcessingDbExceptionHandler : IDbExceptionHandler
{
    /// Transforms database exceptions encountered when using a postgreSQL database into more generic exceptions.
    public Task HandleAsync(DbUpdateExceptionContext context)
    {
        if(context.Exception is not DbUpdateException dbUpdateException)
            return Task.CompletedTask;
        
        if (dbUpdateException.InnerException is not PostgresException postgresException)
            return Task.CompletedTask;
        
        throw new DataProcessingException(postgresException.SqlState == "23505", "Unable to save data", context.Exception);
    }
}