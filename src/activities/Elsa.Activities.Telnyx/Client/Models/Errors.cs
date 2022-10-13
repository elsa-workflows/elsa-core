using System.Collections.Generic;

namespace Elsa.Activities.Telnyx.Client.Models
{
    public class ErrorResponse
    {
        public IList<Error> Errors { get; set; } = default!;
    }

    public class Error
    {
        public int Code { get; set; }
        public string Title { get; set; } = default!;
        public string Detail { get; set; } = default!;
    }

    public static class ErrorCodes
    {
        public const int CallHasAlreadyEnded = 90018;
    }
}