namespace Elsa.Workflows.Api.Security;

internal static class WorkflowDefinitionScriptAuthorizationFailure
{
    public static async Task SendAsync(
        WorkflowDefinitionScriptAuthorizationResult result,
        Func<CancellationToken, Task> sendForbiddenAsync,
        Action<string> addError,
        Func<int, CancellationToken, Task> sendErrorsAsync,
        CancellationToken cancellationToken)
    {
        if (result.FailureReason == WorkflowDefinitionScriptAuthorizationFailureReason.MissingPermission)
        {
            await sendForbiddenAsync(cancellationToken);
            return;
        }

        addError(result.Message!);
        await sendErrorsAsync(400, cancellationToken);
    }
}
