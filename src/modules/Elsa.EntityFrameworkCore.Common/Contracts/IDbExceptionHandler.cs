namespace Elsa.EntityFrameworkCore.Contracts;

/// Defines the contract for an exception handler in a database context.
/// <remarks>
/// <para>Implementing this interface allows for customized handling of exceptions that occur during database operations.</para>
/// </remarks>
public interface IDbExceptionHandler
{
    /// Handles the given exception that occurs during database operations.
    public Task HandleAsync(DbUpdateExceptionContext context);
}