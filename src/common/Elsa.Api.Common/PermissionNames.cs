namespace Elsa;

public static class PermissionNames
{
    public const string All = "*";
    public const string ClaimType = "permissions";

    /// <summary>
    /// Written into Elsa-issued tokens when the caller has zero grants, so a missing
    /// <see cref="ClaimType"/> claim can stay reserved for principals whose permissions are genuinely
    /// unknown (third-party OIDC). Not a grant: <c>Permission.TryParse</c> rejects it, and Studio treats
    /// a present unparseable <c>permissions</c> claim as a known empty set.
    /// </summary>
    /// <remarks>
    /// An empty JSON array or empty-string claim is dropped or expanded to zero claims by ASP.NET Core
    /// JwtBearer and by Studio's WASM JWT parser, which would collapse back to "unknown". A non-empty
    /// string that is not a permission survives both paths.
    /// </remarks>
    public const string None = "none";


    /// <summary>
    /// Permission required to pause, resume, or force-drain the workflow runtime.
    /// </summary>
    public const string ManageWorkflowRuntime = "ManageWorkflowRuntime";

    /// <summary>
    /// Permission required to query workflow runtime status.
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
