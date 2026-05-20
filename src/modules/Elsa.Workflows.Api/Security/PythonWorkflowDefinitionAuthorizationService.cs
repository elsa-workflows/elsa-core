using System.Security.Claims;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Security;

internal class PythonWorkflowDefinitionAuthorizationService(
    IActivityVisitor activityVisitor,
    IExpressionDescriptorRegistry expressionDescriptorRegistry)
{
    public const string HostDisabledMessage = "Python.NET workflow expression execution is disabled by the host. Set PythonOptions.AllowHostCodeExecution to true only for trusted workflow authors; Python.NET is not a sandbox.";
    private const string PythonExpressionType = "Python";
    // Keep in sync with ActivityTypeNameHelper.GenerateTypeName<Elsa.Expressions.Python.Activities.RunPython>().
    private const string RunPythonActivityType = "Elsa.RunPython";
    private const string PermissionsClaimType = "permissions";

    public async Task<PythonWorkflowDefinitionAuthorizationResult> AuthorizeAsync(WorkflowDefinitionModel model, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (model.Root == null || !await UsesPythonAsync(model.Root, cancellationToken))
            return PythonWorkflowDefinitionAuthorizationResult.Allowed;

        return AuthorizePythonUsage(user);
    }

    public async Task<PythonWorkflowDefinitionAuthorizationResult> AuthorizeAsync(Workflow workflow, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (!await UsesPythonAsync(workflow, cancellationToken))
            return PythonWorkflowDefinitionAuthorizationResult.Allowed;

        return AuthorizePythonUsage(user);
    }

    private PythonWorkflowDefinitionAuthorizationResult AuthorizePythonUsage(ClaimsPrincipal user)
    {
        // PythonOptions lives in the optional Python module. Workflows.Api observes the descriptor state projected by that module's provider.
        if (expressionDescriptorRegistry.Find(PythonExpressionType)?.IsBrowsable != true)
            return PythonWorkflowDefinitionAuthorizationResult.HostDisabled;

        return HasPermission(user, PermissionNames.ExecutePythonExpressions)
            ? PythonWorkflowDefinitionAuthorizationResult.Allowed
            : PythonWorkflowDefinitionAuthorizationResult.MissingPermission;
    }

    private async Task<bool> UsesPythonAsync(IActivity root, CancellationToken cancellationToken)
    {
        var graph = await activityVisitor.VisitAsync(root, cancellationToken);
        var nodes = new[] { graph }.Concat(graph.Descendants());

        return nodes.Any(x => IsRunPythonActivity(x.Activity) || HasPythonExpression(x.Activity));
    }

    private static bool IsRunPythonActivity(IActivity activity)
    {
        return string.Equals(activity.Type, RunPythonActivityType, StringComparison.Ordinal);
    }

    private static bool HasPythonExpression(IActivity activity)
    {
        return activity.GetInputs().Any(x => string.Equals(x.Expression?.Type, PythonExpressionType, StringComparison.Ordinal));
    }

    private static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        return user.Claims.Any(x =>
            x.Type == PermissionsClaimType &&
            (string.Equals(x.Value, PermissionNames.All, StringComparison.Ordinal) ||
             string.Equals(x.Value, permission, StringComparison.Ordinal)));
    }
}

internal enum PythonWorkflowDefinitionAuthorizationResult
{
    Allowed,
    HostDisabled,
    MissingPermission
}
