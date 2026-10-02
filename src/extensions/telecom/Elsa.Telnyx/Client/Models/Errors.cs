namespace Elsa.Telnyx.Client.Models;

public record ErrorResponse(IList<Error> Errors);
public record Error(string Code, string Title, string Detail);

public static class ErrorCodes
{
    public const string CallHasAlreadyEnded = "90018";
}