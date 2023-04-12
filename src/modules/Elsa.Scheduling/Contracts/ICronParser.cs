namespace Elsa.Scheduling.Contracts;

/// <summary>Represents a cron expression parser.</summary>
/// <remarks>The providers can support different formats. Quartz, for example, supports years.</remarks>
public interface ICronParser
{
    /// <summary>
    /// Converts a provider dependent cron expression to a <see cref="DateTimeOffset"/>.
    /// </summary>
    /// <param name="expression">The cron expression to parse.</param>
    /// <returns>The next occurrence.</returns>
    DateTimeOffset GetNextOccurrence(string expression);
}