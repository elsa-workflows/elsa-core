namespace Elsa.Common.Helpers;

/// <summary>
/// Extension methods for <see cref="Exception"/>.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Returns <c>true</c> for exceptions that represent unrecoverable process-level failures
    /// and should never be caught and swallowed.
    /// </summary>
    public static bool IsFatal(this Exception exception) =>
        exception is OutOfMemoryException
            or BadImageFormatException
            or InvalidProgramException;
}
