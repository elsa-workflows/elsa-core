using Elsa.Studio.ExternalAuthentication.Models;

namespace Elsa.Studio.ExternalAuthentication.Services;

public static class IdentityLinkUiState
{
    public static IReadOnlyCollection<string> Validate(ExternalIdentityLinkMutationRequest request)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(request.UserId))
            errors.Add("Select an Elsa user.");
        if (string.IsNullOrWhiteSpace(request.ConnectionKey))
            errors.Add("Select an identity provider connection.");
        if (!Uri.TryCreate(request.Issuer, UriKind.Absolute, out var issuer) ||
            issuer.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(issuer.Query) ||
            !string.IsNullOrEmpty(issuer.Fragment))
            errors.Add("Issuer must be an absolute HTTPS URI without a query string or fragment.");
        if (string.IsNullOrWhiteSpace(request.Subject) || request.Subject.Length > 4096)
            errors.Add("Subject is required and must not exceed 4096 characters.");
        return errors;
    }

    public static string SubjectDisplay(ExternalIdentityLink link) =>
        string.IsNullOrWhiteSpace(link.SubjectHint) ? "Stored as a keyed hash" : link.SubjectHint;
}
