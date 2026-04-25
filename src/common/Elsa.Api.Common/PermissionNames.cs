namespace Elsa;

public static class PermissionNames
{
    public const string All = "*";

    /// <summary>
    /// Permission required to pause, resume, force-drain, or query the workflow runtime's graceful-shutdown status.
    /// </summary>
    public const string ManageWorkflowRuntime = "ManageWorkflowRuntime";
}