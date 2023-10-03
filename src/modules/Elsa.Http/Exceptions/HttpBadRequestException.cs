namespace Elsa.Http.Exceptions;

/// <summary>
/// Exception thrown when a bad request is received.
/// </summary>
public class HttpBadRequestException : Exception
{
    /// <inheritdoc />
    public HttpBadRequestException(string message, Exception exception) : base(message, exception)
    {
    }
}