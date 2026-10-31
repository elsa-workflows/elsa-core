namespace Elsa.AI.Persistence.EFCore.Helpers;

internal static class ExceptionFilters
{
    public static bool IsNonFatal(Exception exception) => !IsFatal(exception) && exception is not OperationCanceledException;

    private static bool IsFatal(Exception exception) =>
        exception is OutOfMemoryException and not InsufficientMemoryException
        || exception is StackOverflowException
        || exception is AccessViolationException
        || exception.InnerException is not null && IsFatal(exception.InnerException);
}
