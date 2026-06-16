namespace Elsa;

public static class PermissionNames
{
    public const string All = "*";
    public const string ClaimType = "permissions";

    /// <summary>
    /// Permission required to author or execute C# workflow expressions.
    /// </summary>
    public const string ExecuteCSharpExpressions = "exec:csharp-expressions";

    /// <summary>
    /// Permission required to author or execute Python.NET workflow expressions.
    /// </summary>
    public const string ExecutePythonExpressions = "exec:python-expressions";

    /// <summary>
    /// Permission required to pause, resume, or force-drain the workflow runtime.
    /// </summary>
    public const string ManageWorkflowRuntime = "ManageWorkflowRuntime";

    /// <summary>
    /// Permission required to query the workflow runtime's graceful-shutdown status.
    /// </summary>
    public const string ReadWorkflowRuntime = "read:workflow-runtime";

    /// <summary>
    /// Permission required to list or inspect bookmark queue dead-letter items.
    /// </summary>
    public const string ReadBookmarkQueueDeadLetters = "read:bookmark-queue:dead-letters";

    /// <summary>
    /// Permission required to replay bookmark queue dead-letter items.
    /// </summary>
    public const string ReplayBookmarkQueueDeadLetters = "replay:bookmark-queue:dead-letters";

    /// <summary>
    /// Permission required to delete bookmark queue dead-letter items.
    /// </summary>
    public const string DeleteBookmarkQueueDeadLetters = "delete:bookmark-queue:dead-letters";
}
