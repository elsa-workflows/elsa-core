using System.Security.Claims;
using Elsa.Authorization;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Options;
using Elsa.UserTasks.Permissions;
using Elsa.UserTasks.Services;

namespace Elsa.UserTasks.UnitTests.Authorization;

/// <summary>
/// The policy layer matches grants the way the endpoint gate does.
/// </summary>
/// <remarks>
/// These are the grants the module could not previously honour: it compared claim values for equality, so an
/// administrator who wrote <c>user-tasks:*</c> — a grant the model defines, the catalog implies, and every
/// other module accepts — was silently denied. Every case here failed before the migration, and a regression
/// would be invisible in behaviour that merely looks like a missing grant.
/// </remarks>
public class ActorPermissionMatchingTests
{
    private readonly UserTaskTestFixture _fixture = new();

    [Theory]
    [InlineData("user-tasks:view")]
    [InlineData("user-tasks:*")]
    [InlineData("user-tasks/*:view")]
    [InlineData("*:view")]
    [InlineData("*")]
    public async Task GrantCoveringViewOpensTheAssignedScope(string grant)
    {
        var actor = _fixture.Actor("user-1", grant);

        Assert.NotNull(await _fixture.Policy.CreateScopeAsync(actor, UserTaskQueryScopeKind.Assigned));
    }

    [Theory]
    [InlineData("user-tasks:complete")]
    [InlineData("workflows/*:view")]
    [InlineData("user-tasks/participants:view")]
    public async Task GrantNotCoveringViewDoesNot(string grant)
    {
        var actor = _fixture.Actor("user-1", grant);

        Assert.Null(await _fixture.Policy.CreateScopeAsync(actor, UserTaskQueryScopeKind.Assigned));
    }

    [Fact]
    public async Task VerbWildcardAuthorizesAnOperationItNamesNoVerbFor()
    {
        var actor = _fixture.Actor("user-1", "user-tasks:*");
        var task = await _fixture.ProjectAsync(actor.Subject);

        Assert.True(await _fixture.Policy.AuthorizeAsync(task, actor, UserTaskAccessOperation.Claim));
    }

    [Fact]
    public async Task SubtreeGrantConfersManagerStandingWhenTheHostSetsTheFlag()
    {
        // The tenant-wide scope needs view as well as supervise, and one subtree grant covers both.
        var actor = _fixture.Actor("manager-1", "user-tasks/*:view", "user-tasks/*:supervise") with { IsManager = true };

        Assert.NotNull(await _fixture.Policy.CreateScopeAsync(actor, UserTaskQueryScopeKind.All));
    }

    [Fact]
    public async Task ClaimsResolverDerivesManagerStandingFromAWildcardGrant()
    {
        var resolver = new DefaultClaimsIdentityResolver(Microsoft.Extensions.Options.Options.Create(new UserTasksOptions()));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "user-1"),
            new Claim("permissions", "user-tasks:*")
        ], "test"));

        var actor = await resolver.ResolveAsync(principal);

        Assert.True(actor!.IsManager);
        Assert.True(actor.HasPermission(UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Supervise));
    }

    [Fact]
    public async Task ClaimsResolverWithholdsManagerStandingFromAPlainWorkerGrant()
    {
        var resolver = new DefaultClaimsIdentityResolver(Microsoft.Extensions.Options.Options.Create(new UserTasksOptions()));
        var principal = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("sub", "user-1"),
            new Claim("permissions", "user-tasks:view"),
            new Claim("permissions", "user-tasks:claim")
        ], "test"));

        var actor = await resolver.ResolveAsync(principal);

        Assert.False(actor!.IsManager);
    }

    [Fact]
    public void MalformedGrantsAreIgnoredRatherThanMatched()
    {
        // The legacy spelling is not a well-formed permission on the resource axis: it parses as resource
        // 'read', verb 'user-tasks'. It must therefore authorize nothing here, which is what makes the
        // migration guide's rewrite mandatory rather than advisory.
        var actor = _fixture.Actor("user-1", "read:user-tasks", "not a permission");

        Assert.False(actor.HasPermission(UserTasksResourcePermissions.UserTasks, CoreVerbs.View));
    }

    [Fact]
    public async Task GuestSessionsCarryOnlyViewAndComplete()
    {
        var manager = _fixture.ManagerActor();
        var task = await _fixture.ProjectAsync(_fixture.Actor("user-1").Subject, UserTaskTestFixture.WithBearerInvitation());
        var (guest, _) = await _fixture.IssueGuestSessionAsync(task, manager);

        Assert.True(guest.HasPermission(UserTasksResourcePermissions.UserTasks, CoreVerbs.View));
        Assert.True(guest.HasPermission(UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Complete));
        Assert.False(guest.HasPermission(UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Claim));
        Assert.False(guest.HasPermission(UserTasksResourcePermissions.UserTasks, UserTaskVerbs.Supervise));
    }
}
