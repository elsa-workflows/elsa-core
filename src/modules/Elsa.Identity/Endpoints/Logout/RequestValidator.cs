using FastEndpoints;
using FluentValidation;
using JetBrains.Annotations;

namespace Elsa.Identity.Endpoints.Logout;

[PublicAPI]
internal class RequestValidator : Validator<Request>
{
    public RequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
