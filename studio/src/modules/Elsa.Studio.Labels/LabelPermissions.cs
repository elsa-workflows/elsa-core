namespace Elsa.Studio.Labels;

/// <summary>Backend permission resources guarding the labels APIs.</summary>
public static class LabelPermissions
{
    /// <summary>Labels.</summary>
    public const string Labels = "labels";

    /// <summary>The labels applied to a workflow definition.</summary>
    public const string WorkflowDefinitionLabels = "workflows/definitions/labels";
}
