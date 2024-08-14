using Microsoft.EntityFrameworkCore;

namespace Elsa.EntityFrameworkCore.Common.Contracts;

/// Defines the contract for an exception handler in a database context.
/// <remarks>
/// <para>Implementing this interface allows for customized handling of exceptions that occur during database operations. </para>
/// <para>The <see cref="TDbContext"/> parameter is used to be able to inject different ExceptionHandlers for each DbContext. </para>
/// </remarks>
public interface IDbExceptionHandler<TDbContext> where TDbContext : DbContext
{
    /// Handles the given exception that occurs during database operations.
    public void Handle(DbUpdateException exception);
}