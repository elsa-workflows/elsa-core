using Elsa.Resilience.Contracts;

namespace Elsa.Resilience.Services;

/// <summary>
/// Default implementation of <see cref="ITransientExceptionDetectionService"/> that delegates to registered detectors.
/// </summary>
public class TransientExceptionDetectionService : ITransientExceptionDetectionService
{
    private readonly IEnumerable<ITransientExceptionDetector> _detectors;

    public TransientExceptionDetectionService(IEnumerable<ITransientExceptionDetector> detectors)
    {
        _detectors = detectors;
    }

    /// <inheritdoc />
    public bool IsTransient(Exception exception)
    {
        var detectorsList = _detectors.ToList();

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
