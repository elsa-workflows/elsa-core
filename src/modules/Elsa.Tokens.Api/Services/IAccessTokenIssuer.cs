namespace Elsa.Tokens.Api.Services;

public interface IAccessTokenIssuer
{
    ValueTask<string> CreateTokenAsync(IDictionary<string,string> payload, object context, CancellationToken cancellationToken = default);
}