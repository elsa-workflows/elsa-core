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
        new(
            "CSharp",
            WorkflowScriptActivityTypeNames.RunCSharp,
            "C# workflow expression execution is disabled by the host. Set CSharpOptions.AllowHostCodeExecution to true only for trusted workflow authors; Roslyn scripting is not a sandbox."),
        new(
            "Python",
            WorkflowScriptActivityTypeNames.RunPython,
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

        var failure = scriptUsages
            .Select(policy => AuthorizeScriptUsage(policy, user))
            .FirstOrDefault(result => result is { Succeeded: false });

        if (failure.FailureReason.HasValue)
            return failure;

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

        // The host switch is the single control. The former per-author permission conflated an incoherent
        // execution-side gate -- a workflow runs under the server's authority, not the caller's, so the
        // check never constrained what a script could do -- with a meaningful authoring-side one. Per-author
        // script trust is redesigned separately; see #7975.
        return WorkflowDefinitionScriptAuthorizationResult.Allowed();
    }

    private async Task<IEnumerable<ScriptPolicy>> GetUsedScriptPoliciesAsync(IActivity root, CancellationToken cancellationToken)
    {
        var graph = await activityVisitor.VisitAsync(root, cancellationToken);
        var nodes = new[] { graph }.Concat(graph.Descendants()).ToList();
        var policies = ScriptPolicies
            .Where(policy => nodes.Any(x => IsRunActivity(x.Activity, policy) || HasExpression(x.Activity, policy)))
            .ToList();

        return policies;
    }

    private static bool IsRunActivity(IActivity activity, ScriptPolicy policy) =>
        string.Equals(activity.Type, policy.RunActivityType, StringComparison.Ordinal);

    private static bool HasExpression(IActivity activity, ScriptPolicy policy) =>
        activity.GetInputs().Any(x => string.Equals(x.Expression?.Type, policy.ExpressionType, StringComparison.Ordinal));

    private sealed record ScriptPolicy(string ExpressionType, string RunActivityType, string HostDisabledMessage);
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
