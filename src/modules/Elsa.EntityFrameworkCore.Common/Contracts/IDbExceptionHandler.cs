namespace Elsa.EntityFrameworkCore;

/// <summary>
/// Defines the contract for an exception handler in a database context.
/// </summary>
/// <remarks>
/// <para>Implementing this interface allows for customized handling of exceptions that occur during database operations.</para>
/// </remarks>
public interface IDbExceptionHandler
{
    /// <summary>
    /// Handles the given exception that occurs during database operations.
    /// </summary>
    public Task HandleAsync(DbUpdateExceptionContext context);
}