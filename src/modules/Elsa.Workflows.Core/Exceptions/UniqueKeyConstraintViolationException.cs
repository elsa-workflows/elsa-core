namespace Elsa.Workflows.Exceptions;

/// An exception that when a unique key constraint has been violated.
public class UniqueKeyConstraintViolationException(string message, Exception exception) : Exception(message, exception);
