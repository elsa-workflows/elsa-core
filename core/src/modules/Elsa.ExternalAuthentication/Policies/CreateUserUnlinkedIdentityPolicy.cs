using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using System.Text.Json;

namespace Elsa.ExternalAuthentication.Policies;

/// <summary>
/// Requests just-in-time creation of a credential-less Elsa user for an unlinked external identity.
/// </summary>
public sealed class CreateUserUnlinkedIdentityPolicy : IUnlinkedIdentityPolicy
{
    public const string PolicyType = "create-user";
    private const string DefaultUserNamePrefix = "external";

    public string Type => PolicyType;

    public UnlinkedIdentityPolicyDescriptor Describe() => new(
        Type,
        "Create a user",
        "Creates a credential-less Elsa user and an external identity link after successful external authentication.",
        1,
        [new("defaultRoleIds", "Default roles", "Role IDs assigned only when this policy creates a new user.", "string-array", false, "tags", null, [], new(), false, false, null, null, false)],
        null);

    public ValueTask<UnlinkedIdentityDecision> EvaluateAsync(UnlinkedIdentityContext context, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<UnlinkedIdentityDecision>(new UnlinkedIdentityDecision.CreateUser(new(DefaultUserNamePrefix, DefaultRoleIds: ReadRoleIds(context.Settings))));
    }

    internal static IReadOnlyCollection<string> ReadRoleIds(JsonElement settings) =>
        settings.ValueKind == JsonValueKind.Object && settings.TryGetProperty("defaultRoleIds", out var values) && values.ValueKind == JsonValueKind.Array
            ? values.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()).Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().Distinct(StringComparer.Ordinal).ToArray()
            : [];
}
