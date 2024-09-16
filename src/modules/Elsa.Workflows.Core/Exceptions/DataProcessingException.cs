namespace Elsa.Workflows.Exceptions;

/// An exception that occurs during data processing.
public class DataProcessingException(string message, Exception exception) : Exception(message, exception);
