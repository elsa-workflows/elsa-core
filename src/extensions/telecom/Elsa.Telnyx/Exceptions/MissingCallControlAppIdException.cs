namespace Elsa.Telnyx.Exceptions;

public class MissingCallControlAppIdException(string message, Exception? innerException = null) : TelnyxException(message, innerException);