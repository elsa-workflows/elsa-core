using Elsa.Studio.Authorization;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Extensions;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Each workflow action check requires what its core endpoint requires, so a view-only user never gets the action and a
/// user holding the permission always does. The designer, version history and instance viewer gate on these checks.
/// </summary>
public class UserPermissionsExtensionsTests
{
    private static readonly UserPermissions ViewOnly = StubPermissionService.Grants(
        "workflows/definitions:view", "workflows/instances:view", "alterations:view");

    public static TheoryData<Func<UserPermissions, bool>, string[]> Checks => new()
    {
        { x => x.CanWriteDefinitions(), ["workflows/definitions:write"] },
        { x => x.CanDeleteDefinitions(), ["workflows/definitions:delete"] },
        { x => x.CanPublishDefinitions(), ["workflows/definitions:publish"] },
        { x => x.CanRetractDefinitions(), ["workflows/definitions:retract"] },
        { x => x.CanExecuteDefinitions(), ["workflows/definitions:execute"] },
        { x => x.CanRevertDefinitionVersions(), ["workflows/definitions/versions:revert"] },
        { x => x.CanRunActivityTests(), ["workflows/tests:execute"] },
        { x => x.CanDeleteInstances(), ["workflows/instances:delete"] },
        { x => x.CanCancelInstances(), ["workflows/instances:cancel"] },
        { x => x.CanImportInstances(), ["workflows/instances:write"] },
        { x => x.CanExecuteAlterations(), ["alterations:execute"] },
        { x => x.CanAlterInstances(), ["alterations:execute", "workflows/instances:view", "workflows/definitions:view"] },
    };

    [Theory]
    [MemberData(nameof(Checks))]
    public void Check_HoldsExactlyWithTheEndpointsPermissions(Func<UserPermissions, bool> check, string[] required)
    {
        Assert.True(check(StubPermissionService.Grants(required)));
        Assert.False(check(ViewOnly));

        // Every required permission counts: dropping any one of them fails the check.
        Assert.All(required, missing => Assert.False(check(StubPermissionService.Grants(required.Where(x => x != missing).ToArray()))));
    }

    [Fact]
    public void Checks_PassWhenPermissionsAreUnknown()
    {
        Assert.All(Checks, row => Assert.True(((Func<UserPermissions, bool>)row[0])(UserPermissions.Unknown)));
    }
}
