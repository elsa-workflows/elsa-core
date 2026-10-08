using Elsa.Authorization;
using Elsa.Permissions;
using JetBrains.Annotations;

namespace Elsa.Http.Webhooks.Permissions;

/// <summary>
/// Stable resource names for webhook administration. The inbound <c>POST /webhooks</c> event sink
/// stays anonymous; this resource gates Studio's Webhooks page and menu.
/// </summary>
public static class WebhookPermissions
{
    /// <summary>Manage webhook sinks and sources from Studio.</summary>
    public const string Webhooks = "http/webhooks";
}

/// <summary>Contributes the Webhooks resource to the permission catalog so <c>*</c> administrators receive it.</summary>
[UsedImplicitly]
public sealed class WebhookPermissionsDescriptorProvider : IPermissionDescriptorProvider
{
    /// <inheritdoc />
    public IEnumerable<PermissionDescriptor> GetDescriptors() =>
    [
        new(WebhookPermissions.Webhooks, [CoreVerbs.View, CoreVerbs.Create, CoreVerbs.Update, CoreVerbs.Delete], "Webhooks", "Manage webhook sinks and sources.", "HTTP"),
    ];
}
