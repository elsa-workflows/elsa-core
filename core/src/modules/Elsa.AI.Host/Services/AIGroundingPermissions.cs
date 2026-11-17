using Elsa.AI.Abstractions.Models;
using Elsa.Authorization;

namespace Elsa.AI.Host.Services;

internal static class AIGroundingPermissions
{
    public const string DefinitionView = "workflows/definitions:view";
    public const string InstanceView = "workflows/instances:view";
    public const string ActivityExecutionView = "workflows/activity-executions:view";
    public const string DefinitionWrite = "workflows/definitions:write";

    private static readonly Dictionary<string, string> AttachmentPermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        [AIContextAttachmentKinds.WorkflowDefinition] = DefinitionView,
        [AIContextAttachmentKinds.Activity] = DefinitionView,
        [AIContextAttachmentKinds.WorkflowInstance] = InstanceView
    };

    public static string? ForAttachment(string kind) => AttachmentPermissions.GetValueOrDefault(kind);

    public static bool IsGroundingPermission(string permission) =>
        permission is DefinitionView or InstanceView or ActivityExecutionView or DefinitionWrite;

    public static bool HasAll(IEnumerable<string> claims, IEnumerable<string> required)
    {
        var permissions = new List<Permission>();
        foreach (var claim in claims)
            if (Permission.TryParse(claim, out var permission))
                permissions.Add(permission);

        return required.All(requirement =>
            Permission.TryParse(requirement, out var permission) && PermissionMatcher.Satisfies(permissions, permission));
    }
}
