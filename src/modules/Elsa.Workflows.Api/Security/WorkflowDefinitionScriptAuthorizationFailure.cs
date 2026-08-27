namespace Elsa.Workflows.Api.Security;

internal static class WorkflowDefinitionScriptAuthorizationFailure
{
    public static async Task SendAsync(
        WorkflowDefinitionScriptAuthorizationResult result,
        Action<string> addError,
        Func<int, CancellationToken, Task> sendErrorsAsync,
        CancellationToken cancellationToken)
    {
        // Only one failure is possible: the host has the language switched off. That is a property of the
        // deployment rather than of the caller, so it is a 400 explaining what is disabled, never a 403.
        addError(result.Message ?? "Workflow script authorization failed.");
        await sendErrorsAsync(400, cancellationToken);
    }
}
