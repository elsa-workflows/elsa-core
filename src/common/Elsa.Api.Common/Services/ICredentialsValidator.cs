using Elsa.Api.Common.Models;

namespace Elsa.Api.Common.Services;

public interface ICredentialsValidator
{
    ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(IDictionary<string, string> credentials, CancellationToken cancellationToken = default);
}