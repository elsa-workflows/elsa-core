using Elsa.Studio.Workflows;

namespace Elsa.Studio.Alterations;

/// <summary>Backend permission resources guarding the alterations APIs.</summary>
public static class AlterationPermissions
{
    /// <summary>Alteration plans: <c>view</c> inspects them, <c>execute</c> submits them.</summary>
    public const string Alterations = WorkflowPermissions.Alterations;
}
