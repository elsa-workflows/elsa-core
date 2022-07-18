using Elsa.Api.Common.Models;
using Elsa.Api.Common.Services;
using Elsa.WorkflowServer.Web.Models;

namespace Elsa.WorkflowServer.Web.Implementations;

public class CustomCredentialsValidator : ICredentialsValidator
{
    public ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(IDictionary<string, string> credentials, CancellationToken cancellationToken = default)
    {
        var isValid = credentials.TryGetValue("user", out var user) && user == "admin" && credentials.TryGetValue("password", out var password) && password == "admin";

        return !isValid
            ? new(ValidateCredentialsResult.Invalid())
            : new(ValidateCredentialsResult.Valid(new User("Superman")));
    }
}