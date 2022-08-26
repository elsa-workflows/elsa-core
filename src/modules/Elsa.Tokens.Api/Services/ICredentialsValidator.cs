using Elsa.Tokens.Api.Models;

namespace Elsa.Tokens.Api.Services;

public interface ICredentialsValidator
{
    ValueTask<CredentialValidationResult> ValidateCredentialsAsync(IDictionary<string,string> credentials, CancellationToken cancellationToken = default);
}