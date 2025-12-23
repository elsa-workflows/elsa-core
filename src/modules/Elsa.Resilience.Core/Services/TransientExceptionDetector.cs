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

        // Walk the entire exception chain (including inner exceptions)
        var currentException = exception;
        while (currentException != null)
        {
            // Check if any detector identifies this exception as transient
            if (detectorsList.Any(detector => detector.IsTransient(currentException)))
                return true;

            currentException = currentException.InnerException;
        }

        // Also check aggregate exceptions
        if (exception is AggregateException aggregateException)
        {
            foreach (var innerException in aggregateException.InnerExceptions)
            {
                if (IsTransient(innerException))
                    return true;
            }
        }

        return false;
    }
}
