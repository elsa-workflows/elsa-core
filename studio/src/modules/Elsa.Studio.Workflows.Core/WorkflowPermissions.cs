namespace Elsa.Studio.Workflows;

/// <summary>Backend permission resources guarding the workflow APIs.</summary>
public static class WorkflowPermissions
{
    /// <summary>Workflow definitions.</summary>
    public const string Definitions = "workflows/definitions";

    /// <summary>Individual workflow definition versions: reverting to one requires <c>revert</c>.</summary>
    public const string DefinitionVersions = "workflows/definitions/versions";

    /// <summary>Workflow instances.</summary>
    public const string Instances = "workflows/instances";

    /// <summary>The workflow runtime, whose status the dashboard shows.</summary>
    public const string Runtime = "workflows/runtime";

    /// <summary>Activity tests run from the designer.</summary>
    public const string Tests = "workflows/tests";

    /// <summary>Alteration plans, which the workflow instance views submit and link to.</summary>
    public const string Alterations = "alterations";
}

/// <summary>The workflow verbs beyond the core <c>PermissionVerbs</c>.</summary>
public static class WorkflowVerbs
{
    /// <summary>Publish a workflow definition.</summary>
    public const string Publish = "publish";

    /// <summary>Unpublish (retract) a workflow definition.</summary>
    public const string Retract = "retract";

    /// <summary>Revert a workflow definition to an earlier version.</summary>
    public const string Revert = "revert";

    /// <summary>Cancel running workflow instances.</summary>
    public const string Cancel = "cancel";
}
