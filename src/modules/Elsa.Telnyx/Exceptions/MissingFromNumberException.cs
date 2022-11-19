namespace Elsa.Telnyx.Exceptions;

public class MissingFromNumberException : TelnyxException
{
    public MissingFromNumberException(string message, Exception? innerException = default) : base(message, innerException)
    {
    }
}