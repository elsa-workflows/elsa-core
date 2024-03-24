using Elsa.Abstractions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime.Contracts;
using Elsa.Workflows.Runtime.Parameters;
using JetBrains.Annotations;

namespace Elsa.Server.Web.Endpoints.Api1.Get;

/// <summary>
/// Returns a message.
/// </summary>
[UsedImplicitly]
public class Get : ElsaEndpointWithoutRequest
{
    /// <inheritdoc />
    public override void Configure()
    {
        Get("/api-1");
        AllowAnonymous();
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        await Task.Delay(1000, ct);
        var response = new
        {
            Message = "OK"
        };
        await SendOkAsync(response, ct);
    }
}