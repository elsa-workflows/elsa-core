using Elsa.Studio.Authorization;

namespace Elsa.Studio.Workflows.Extensions;

/// <summary>
/// Workflow-specific permission checks, each matching what the core endpoint behind the action requires.
/// </summary>
public static class UserPermissionsExtensions
{
    /// <summary>Whether the user can create, import, duplicate and save workflow definitions.</summary>
    public static bool CanWriteDefinitions(this UserPermissions permissions) => permissions.Has(WorkflowPermissions.Definitions, PermissionVerbs.Write);

    /// <summary>Whether the user can delete workflow definitions and their versions.</summary>
    public static bool CanDeleteDefinitions(this UserPermissions permissions) => permissions.Has(WorkflowPermissions.Definitions, PermissionVerbs.Delete);

    /// <summary>Whether the user can publish workflow definitions.</summary>
    public static bool CanPublishDefinitions(this UserPermissions permissions) => permissions.Has(WorkflowPermissions.Definitions, WorkflowVerbs.Publish);

    /// <summary>Whether the user can unpublish workflow definitions.</summary>
    public static bool CanRetractDefinitions(this UserPermissions permissions) => permissions.Has(WorkflowPermissions.Definitions, WorkflowVerbs.Retract);

    /// <summary>Whether the user can run workflow definitions.</summary>
    public static bool CanExecuteDefinitions(this UserPermissions permissions) => permissions.Has(WorkflowPermissions.Definitions, PermissionVerbs.Execute);

    /// <summary>Whether the user can roll a workflow definition back to an earlier version.</summary>
    public static bool CanRevertDefinitionVersions(this UserPermissions permissions) => permissions.Has(WorkflowPermissions.DefinitionVersions, WorkflowVerbs.Revert);

    /// <summary>Whether the user can test an activity from the designer.</summary>
    public static bool CanRunActivityTests(this UserPermissions permissions) => permissions.Has(WorkflowPermissions.Tests, PermissionVerbs.Execute);

    /// <summary>Whether the user can delete workflow instances.</summary>
    public static bool CanDeleteInstances(this UserPermissions permissions) => permissions.Has(WorkflowPermissions.Instances, PermissionVerbs.Delete);

    /// <summary>Whether the user can cancel workflow instances.</summary>
    public static bool CanCancelInstances(this UserPermissions permissions) => permissions.Has(WorkflowPermissions.Instances, WorkflowVerbs.Cancel);

    /// <summary>Whether the user can import workflow instances.</summary>
    public static bool CanImportInstances(this UserPermissions permissions) => permissions.Has(WorkflowPermissions.Instances, PermissionVerbs.Write);

    /// <summary>Whether the user can submit alteration plans, such as a bulk cancel across every matching instance.</summary>
    public static bool CanExecuteAlterations(this UserPermissions permissions) => permissions.Has(WorkflowPermissions.Alterations, PermissionVerbs.Execute);

    /// <summary>
    /// Whether the user can open a workflow instance in the alterations editor, whose page requires alterations:execute
    /// as well as viewing the instance and its definition.
    /// </summary>
    public static bool CanAlterInstances(this UserPermissions permissions) =>
        permissions.CanExecuteAlterations()
        && permissions.Has(WorkflowPermissions.Instances, PermissionVerbs.View)
        && permissions.Has(WorkflowPermissions.Definitions, PermissionVerbs.View);
}
