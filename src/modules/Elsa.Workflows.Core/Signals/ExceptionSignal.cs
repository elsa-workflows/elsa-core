using Elsa.Workflows.Contracts;

namespace Elsa.Workflows.Signals;

/// <summary>
/// /// Sent by ExceptionHandlingMiddleware to notify their composite container that it has exception.
/// </summary>
/// <param name="Activity">The exception activity</param>
/// <param name="Exception">The exception</param>
public record ExceptionSignal(IActivity Activity, Exception Exception);