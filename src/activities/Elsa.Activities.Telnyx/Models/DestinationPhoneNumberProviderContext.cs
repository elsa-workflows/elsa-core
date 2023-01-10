using System.Threading;
using Elsa.Services.Models;

namespace Elsa.Activities.Telnyx.Models;

public record DestinationPhoneNumberProviderContext(ActivityExecutionContext ActivityExecutionContext, object? Input)
{
    public CancellationToken CancellationToken => ActivityExecutionContext.CancellationToken;
}