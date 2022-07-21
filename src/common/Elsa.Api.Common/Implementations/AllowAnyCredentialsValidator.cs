using Elsa.Api.Common.Models;
using Elsa.Api.Common.Services;

namespace Elsa.Api.Common.Implementations;

public class AllowAnyCredentialsValidator : ICredentialsValidator
{
    public ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(IDictionary<string, string> credentials, CancellationToken cancellationToken = default) => new(ValidateCredentialsResult.Valid());
}