using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;

namespace Elsa.Authorization;

/// <summary>
/// Evaluates <see cref="PermissionRequirement"/> through <see cref="IPermissionEvaluator"/>, replacing the
/// exact-string permission check so that wildcards and the resource hierarchy behave as declared.
/// </summary>
[UsedImplicitly]
public sealed class PermissionAuthorizationHandler(IPermissionEvaluator evaluator) : AuthorizationHandler<PermissionRequirement>
{
    /// <inheritdoc />
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (evaluator.HasPermission(context.User, requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
