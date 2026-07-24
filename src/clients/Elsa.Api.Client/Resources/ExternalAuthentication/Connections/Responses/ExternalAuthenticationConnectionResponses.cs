using Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Models;

namespace Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Responses;

public sealed class ListExternalAuthenticationConnectionsResponse
{
    public ICollection<ExternalAuthenticationConnection> Items { get; set; } = [];
    public string? NextCursor { get; set; }
}

public sealed class ValidateExternalAuthenticationConnectionResponse
{
    public bool Valid { get; set; }
    public ICollection<ExternalAuthenticationConnectionValidationError> Errors { get; set; } = [];
    public ICollection<string> Warnings { get; set; } = [];
}

public sealed class ExternalAuthenticationConnectionValidationError
{
    public string Field { get; set; } = "";
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
}
