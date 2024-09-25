namespace Elsa.Workflows.Contracts;

/// <summary>
/// Generates logger state for the specified <typeparamref name="TContext"/>.
/// </summary>
public interface ILoggerStateGenerator<TContext>
{
    /// <summary>
    /// Generates logger state for the specified <typeparamref name="TContext"/>.
    /// </summary>
    /// <param name="context">The <typeparamref name="TContext"/> to generate logger state for.</param>
    /// <returns>A <see cref="Dictionary{String, Object}" /> containing the state related to the <typeparamref name="TContext"/>.</returns>
    Dictionary<string, object> GenerateLoggerState(TContext context);
}
