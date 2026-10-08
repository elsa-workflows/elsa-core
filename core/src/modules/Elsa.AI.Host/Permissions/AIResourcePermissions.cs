using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.AI.Host.Permissions;

/// <summary>
/// Stable resource names for AI. Endpoints reference these constants rather than string
/// literals, and the descriptors below are declared alongside them so the two cannot drift.
/// </summary>
public static class AIResourcePermissions
{
    /// <summary>Converse with the AI assistant.</summary>
    public const string Chat = "ai/chat";
    /// <summary>Browse available AI tools.</summary>
    public const string Tools = "ai/tools";
    /// <summary>Inspect AI capabilities.</summary>
    public const string Capabilities = "ai/capabilities";
}

/// <summary>Contributes the AI resources to the permission catalog.</summary>
[UsedImplicitly]
public sealed class AIResourcePermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(AIResourcePermissions.Chat, [CoreVerbs.Execute], "AI chat", "Converse with the AI assistant.", "AI"),
        new(AIResourcePermissions.Tools, [CoreVerbs.View], "AI tools", "Browse available AI tools.", "AI"),
        new(AIResourcePermissions.Capabilities, [CoreVerbs.View], "AI capabilities", "Inspect AI capabilities.", "AI"),
    ];
}
