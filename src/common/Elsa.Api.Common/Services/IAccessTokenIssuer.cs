namespace Elsa.Api.Common.Services;

/// <summary>
/// Implement this to issue new security tokens based on the specified credentials.
/// </summary>
public interface IAccessTokenIssuer
{
    ValueTask<string> CreateTokenAsync(IDictionary<string, string> credentials, object? context, CancellationToken cancellationToken = default);
}