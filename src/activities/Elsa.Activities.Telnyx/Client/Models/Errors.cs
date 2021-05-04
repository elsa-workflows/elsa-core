using System.Collections.Generic;

namespace Elsa.Activities.Telnyx.Client.Models
{
    public record ErrorResponse(IList<Error> Errors);
    public record Error(int Code, string Title, string Detail);

    public static class ErrorCodes
    {
        public const int CallHasAlreadyEnded = 90018;
    }
}