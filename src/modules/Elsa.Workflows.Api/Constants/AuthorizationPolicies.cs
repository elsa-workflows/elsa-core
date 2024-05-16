namespace Elsa.Workflows.Api.Constants;

public static class AuthorizationPolicies
{
    /// <summary>
    /// The read-only policy ensures that that workflow management is not possible when the read-only mode is enabled or when a workflow is marked as read-only.
    /// </summary>
    public const string ReadOnlyPolicy = "ReadOnlyPolicy";
}
