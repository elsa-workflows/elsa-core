using System.Text.Json;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Policies;

/// <summary>Delegates external-to-Elsa identity matching to one trusted matcher and applies a safe configured no-match action.</summary>
public sealed class MatchExternalUserUnlinkedIdentityPolicy(IExternalUserMatcherRegistry matchers) : IUnlinkedIdentityPolicy
{
    public const string PolicyType = "match-user";
    public string Type => PolicyType;

    public UnlinkedIdentityPolicyDescriptor Describe() => new(
        Type,
        "Match an existing user",
        "Uses one deployment-installed user matcher, then rejects or creates a user when no match is returned.",
        1,
        [
            new("matcher", "User matcher", "Versioned matcher selection and settings.", "json", true, "json", null, [], new(), false, false, null, null, false),
            new("noMatchAction", "No-match action", "Reject access or create a credential-less user.", "string", true, "select", null, ["reject", "create-user"], new(), false, false, null, null, false),
            new("defaultRoleIds", "Default roles", "Role IDs applied only when the no-match action creates a user.", "string-array", false, "tags", null, [], new(), false, false, null, null, false)
        ],
        null);

    public async ValueTask<UnlinkedIdentityDecision> EvaluateAsync(UnlinkedIdentityContext context, CancellationToken cancellationToken = default)
    {
        if (!TryReadMatcher(context.Settings, out var selection) || !matchers.TryGet(selection.Type, out var matcher))
            return new UnlinkedIdentityDecision.Reject("identity_unlinked");
        var descriptor = matchers.ListDescriptors().FirstOrDefault(x => string.Equals(x.Type, selection.Type, StringComparison.Ordinal));
        if (descriptor is null || descriptor.SettingsVersion != selection.SettingsVersion)
            return new UnlinkedIdentityDecision.Reject("identity_unlinked");

        var requiredClaims = (descriptor.RequiredClaimTypes ?? []).ToHashSet(StringComparer.Ordinal);
        var claims = context.ProjectedClaims.Where(x => requiredClaims.Contains(x.Key)).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        var match = await matcher.MatchAsync(new ExternalUserMatcherContext(context.TargetTenantId, context.Connection, context.Identity, claims, selection.Settings), cancellationToken);
        if (match is ExternalUserMatchResult.Match { UserId: { Length: > 0 } userId, AuthorizationBasis: { Length: > 0 } basis })
            return new UnlinkedIdentityDecision.LinkExistingUser(userId, basis);

        if (match is not ExternalUserMatchResult.NoMatch)
            return new UnlinkedIdentityDecision.Reject("identity_unlinked");

        return string.Equals(ReadString(context.Settings, "noMatchAction"), "create-user", StringComparison.OrdinalIgnoreCase)
            ? new UnlinkedIdentityDecision.CreateUser(new UserCreationProposal("external", DefaultRoleIds: CreateUserUnlinkedIdentityPolicy.ReadRoleIds(context.Settings)))
            : new UnlinkedIdentityDecision.Reject("identity_unlinked");
    }

    private static bool TryReadMatcher(JsonElement settings, out MatcherSelection selection)
    {
        selection = default!;
        if (settings.ValueKind != JsonValueKind.Object || !settings.TryGetProperty("matcher", out var value) || value.ValueKind != JsonValueKind.Object)
            return false;
        var type = ReadString(value, "type");
        var version = value.TryGetProperty("settingsVersion", out var versionValue) && versionValue.TryGetInt32(out var parsed) ? parsed : 0;
        var matcherSettings = value.TryGetProperty("settings", out var matcherSettingsValue) ? matcherSettingsValue.Clone() : default;
        if (string.IsNullOrWhiteSpace(type) || version <= 0)
            return false;
        selection = new MatcherSelection(type, version, matcherSettings);
        return true;
    }

    private static string? ReadString(JsonElement value, string name) => value.ValueKind == JsonValueKind.Object && value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    private sealed record MatcherSelection(string Type, int SettingsVersion, JsonElement Settings);
}
