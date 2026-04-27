using System.Runtime.InteropServices;

namespace Elsa.Common;

/// <summary>
/// Helpers for classifying exceptions during best-effort error handling.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Returns <c>true</c> when <paramref name="exception"/> represents a process-fatal condition that should
    /// NOT be swallowed even by best-effort code paths. Catches that filter on <c>!ex.IsFatal()</c> let normal
    /// failures through for logging-and-continue while letting fatal exceptions propagate to crash the process
    /// cleanly (the host's failure-fast policy can then decide what to do).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Conditions classified as fatal:
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="OutOfMemoryException"/> (when not <see cref="InsufficientMemoryException"/>, which is recoverable)</item>
    ///   <item><see cref="StackOverflowException"/></item>
    ///   <item><see cref="AccessViolationException"/></item>
    ///   <item><see cref="SEHException"/></item>
    ///   <item><see cref="ThreadAbortException"/></item>
    /// </list>
    /// <para>
    /// Wrapper exceptions (<see cref="TypeInitializationException"/>, <see cref="System.Reflection.TargetInvocationException"/>)
    /// are unwrapped before classification so that, for example, a <see cref="TypeInitializationException"/>
    /// wrapping a <see cref="StackOverflowException"/> is classified as fatal.
    /// </para>
    /// <para>
    /// Pattern: use as a filter on a generic <c>catch</c> where the surrounding logic must remain best-effort
    /// for non-fatal errors:
    /// <code>
    /// try { /* best-effort work */ }
    /// catch (Exception ex) when (!ex.IsFatal())
    /// {
    ///     logger.LogError(ex, "...");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static bool IsFatal(this Exception? exception)
    {
        while (exception is not null)
        {
            switch (exception)
            {
                case StackOverflowException:
                case AccessViolationException:
                case SEHException:
                case ThreadAbortException:
                    return true;
                case OutOfMemoryException when exception is not InsufficientMemoryException:
                    // OOM is fatal, but InsufficientMemoryException (its derived form) is recoverable by design.
                    return true;
            }

            // Unwrap reflection-style wrappers so a fatal cause buried inside a TypeInitializationException is still
            // classified as fatal.
            exception = exception switch
            {
                TypeInitializationException tie => tie.InnerException,
                System.Reflection.TargetInvocationException tie => tie.InnerException,
                _ => null,
            };
        }
        return false;
    }
}
