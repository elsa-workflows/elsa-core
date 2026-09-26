namespace Elsa.Telnyx.Exceptions;

public class TelnyxException(string message, Exception? innerException = null) : Exception(message, innerException);