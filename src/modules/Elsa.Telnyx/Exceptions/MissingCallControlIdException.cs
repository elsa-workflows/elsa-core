namespace Elsa.Telnyx.Exceptions;

public class MissingCallControlIdException : TelnyxException
{
    public MissingCallControlIdException(string message, Exception? innerException = default) : base(message, innerException)
    {
    }
}