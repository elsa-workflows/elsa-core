namespace Elsa.Workflows.Api.Security;

internal static class PythonWorkflowDefinitionAuthorizationFailure
{
    public static async Task SendAsync(
        PythonWorkflowDefinitionAuthorizationResult result,
        Func<CancellationToken, Task> sendForbiddenAsync,
        Action<string> addError,
        Func<int, CancellationToken, Task> sendErrorsAsync,
        CancellationToken cancellationToken)
    {
        if (result == PythonWorkflowDefinitionAuthorizationResult.MissingPermission)
        {
            await sendForbiddenAsync(cancellationToken);
            return;
        }

        addError(PythonWorkflowDefinitionAuthorizationService.HostDisabledMessage);
        await sendErrorsAsync(400, cancellationToken);
    }
}
