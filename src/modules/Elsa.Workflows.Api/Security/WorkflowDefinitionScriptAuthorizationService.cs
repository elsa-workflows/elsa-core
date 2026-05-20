using System.Security.Claims;
using Elsa.Expressions.Contracts;
using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Api.Security;

internal class WorkflowDefinitionScriptAuthorizationService(
    IActivityVisitor activityVisitor,
    IExpressionDescriptorRegistry expressionDescriptorRegistry)
{
    private static readonly ScriptPolicy[] ScriptPolicies =
    [
        // Keep run activity type names in sync with ActivityTypeNameHelper.GenerateTypeName<RunCSharp>() and GenerateTypeName<RunPython>().
        new(
            "CSharp",
            "Elsa.RunCSharp",
            PermissionNames.ExecuteCSharpExpressions,
            "C# workflow expression execution is disabled by the host. Set CSharpOptions.AllowHostCodeExecution to true only for trusted workflow authors; Roslyn scripting is not a sandbox."),
        new(
            "Python",
            "Elsa.RunPython",
            PermissionNames.ExecutePythonExpressions,
            "Python.NET workflow expression execution is disabled by the host. Set PythonOptions.AllowHostCodeExecution to true only for trusted workflow authors; Python.NET is not a sandbox.")
    ];

    public async Task<WorkflowDefinitionScriptAuthorizationResult> AuthorizeAsync(WorkflowDefinitionModel model, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        if (model.Root == null)
            return WorkflowDefinitionScriptAuthorizationResult.Allowed();

        return await AuthorizeAsync(model.Root, user, cancellationToken);
    }

    public async Task<WorkflowDefinitionScriptAuthorizationResult> AuthorizeAsync(IActivity root, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var scriptUsages = await GetUsedScriptPoliciesAsync(root, cancellationToken);

        foreach (var policy in scriptUsages)
        {
            var result = AuthorizeScriptUsage(policy, user);
            if (!result.Succeeded)
                return result;
        }

        return WorkflowDefinitionScriptAuthorizationResult.Allowed();
    }

    public async Task<WorkflowDefinitionScriptAuthorizationResult> AuthorizeAsync(Workflow workflow, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        return await AuthorizeAsync((IActivity)workflow, user, cancellationToken);
    }

    private WorkflowDefinitionScriptAuthorizationResult AuthorizeScriptUsage(ScriptPolicy policy, ClaimsPrincipal user)
    {
        // Language-specific options live in optional modules. Workflows.Api observes the descriptor state projected by those module providers.
        if (expressionDescriptorRegistry.Find(policy.ExpressionType)?.IsBrowsable != true)
            return WorkflowDefinitionScriptAuthorizationResult.HostDisabled(policy.HostDisabledMessage);

        return HasPermission(user, policy.Permission)
            ? WorkflowDefinitionScriptAuthorizationResult.Allowed()
            : WorkflowDefinitionScriptAuthorizationResult.MissingPermission();
    }

    private async Task<IEnumerable<ScriptPolicy>> GetUsedScriptPoliciesAsync(IActivity root, CancellationToken cancellationToken)
    {
        var graph = await activityVisitor.VisitAsync(root, cancellationToken);
        var nodes = new[] { graph }.Concat(graph.Descendants()).ToList();
        var policies = new List<ScriptPolicy>();

        foreach (var policy in ScriptPolicies)
        {
            if (nodes.Any(x => IsRunActivity(x.Activity, policy) || HasExpression(x.Activity, policy)))
                policies.Add(policy);
        }

        return policies;
    }

    private static bool IsRunActivity(IActivity activity, ScriptPolicy policy) =>
        string.Equals(activity.Type, policy.RunActivityType, StringComparison.Ordinal);

    private static bool HasExpression(IActivity activity, ScriptPolicy policy) =>
        activity.GetInputs().Any(x => string.Equals(x.Expression?.Type, policy.ExpressionType, StringComparison.Ordinal));

    private static bool HasPermission(ClaimsPrincipal user, string permission)
    {
        return user.Claims.Any(x =>
            x.Type == PermissionNames.ClaimType &&
            (string.Equals(x.Value, PermissionNames.All, StringComparison.Ordinal) ||
             string.Equals(x.Value, permission, StringComparison.Ordinal)));
    }

    private sealed record ScriptPolicy(string ExpressionType, string RunActivityType, string Permission, string HostDisabledMessage);
}

internal readonly record struct WorkflowDefinitionScriptAuthorizationResult(bool Succeeded, WorkflowDefinitionScriptAuthorizationFailureReason? FailureReason, string? Message)
{
    public static WorkflowDefinitionScriptAuthorizationResult Allowed() => new(true, null, null);

    public static WorkflowDefinitionScriptAuthorizationResult HostDisabled(string message) => new(false, WorkflowDefinitionScriptAuthorizationFailureReason.HostDisabled, message);

    public static WorkflowDefinitionScriptAuthorizationResult MissingPermission() => new(false, WorkflowDefinitionScriptAuthorizationFailureReason.MissingPermission, null);
}

internal enum WorkflowDefinitionScriptAuthorizationFailureReason
{
    HostDisabled,
    MissingPermission
}
