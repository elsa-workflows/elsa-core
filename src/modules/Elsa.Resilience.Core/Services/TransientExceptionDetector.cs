namespace Elsa.Resilience;

/// <summary>
/// Default implementation of <see cref="ITransientExceptionDetector"/> that delegates to registered detectors.
/// </summary>
public class TransientExceptionDetector(IEnumerable<ITransientExceptionStrategy> detectors) : ITransientExceptionDetector
{
    /// <inheritdoc />
    public bool IsTransient(Exception exception)
    {
        var detectorsList = detectors.ToList();

        // Handle aggregate exceptions specially to avoid redundant checks
        if (exception is AggregateException aggregateException)
        {
            // Check the aggregate exception itself
            if (detectorsList.Any(detector => detector.IsTransient(aggregateException)))
                return true;

            // Recursively check each inner exception (this will walk their chains)
            if (aggregateException.InnerExceptions.Any(IsTransient))
                return true;

            return false;
        }

        // Walk the exception chain for non-aggregate exceptions
        var currentException = exception;
        while (currentException != null)
        {
            // Check if any detector identifies this exception as transient
            if (detectorsList.Any(detector => detector.IsTransient(currentException)))
                return true;

            currentException = currentException.InnerException;
        }

        return false;
    }
}
