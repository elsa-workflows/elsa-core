namespace Elsa.Studio.ExternalAuthentication.Models;

public sealed record ExternalIdentityLink(
    string Id,
    string UserId,
    string ConnectionKey,
    string Issuer,
    string? SubjectHint,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastSignedInAt);

public sealed record IdentityLinkUser(string Id, string DisplayName);

public sealed record ListExternalIdentityLinksResponse(IReadOnlyCollection<ExternalIdentityLink> Items, string? NextCursor);
public sealed record FindIdentityLinkUsersResponse(IReadOnlyCollection<IdentityLinkUser> Items, string? NextCursor);

public enum IdentityLinkDialogOutcome
{
    Saved,
    Stale
}

public abstract class ExternalIdentityLinkMutationRequest
{
    public string UserId { get; set; } = string.Empty;
    public string ConnectionKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
}

public sealed class PrelinkExternalIdentityRequest : ExternalIdentityLinkMutationRequest;

public sealed class ReplaceExternalIdentityLinkRequest : ExternalIdentityLinkMutationRequest;
