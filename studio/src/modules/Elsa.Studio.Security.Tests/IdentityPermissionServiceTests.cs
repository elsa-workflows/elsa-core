using Elsa.Studio.Authorization;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Elsa.Studio.Services;
using Xunit;

namespace Elsa.Studio.Security.Tests;

/// <summary>
/// Pins that Studio's production permission adapter fails closed: an expired token (401) or
/// anonymous/forbidden principal becomes a known empty grant set, not <see cref="UserPermissions.Unknown"/>.
/// Refs elsa-studio#1107 and #1111.
/// </summary>
public sealed class IdentityPermissionServiceTests
{
    [Fact]
    public async Task GetPermissionsAsync_WhenSnapshotIsReady_ReturnsThoseGrantsOnly()
    {
        var context = new StaticIdentityPermissionContext(new IdentityPermissionSnapshot(
            IdentityPermissionSnapshotState.Ready,
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                ["secrets"] = new HashSet<string>(StringComparer.Ordinal) { "view" }
            }));

        var permissions = await new IdentityPermissionService(context).GetPermissionsAsync();

        Assert.True(permissions.IsKnown);
        Assert.True(permissions.Has("secrets", "view"));
        Assert.False(permissions.Has("secrets", "delete"));
        Assert.False(permissions.Has("identity/users", "view"));
    }

    [Theory]
    [InlineData(IdentityPermissionSnapshotState.Forbidden)]
    [InlineData(IdentityPermissionSnapshotState.Unavailable)]
    public async Task GetPermissionsAsync_WhenSnapshotIsNotReady_FailsClosedAsKnownEmpty(IdentityPermissionSnapshotState state)
    {
        var snapshot = state == IdentityPermissionSnapshotState.Forbidden
            ? IdentityPermissionSnapshot.Forbidden
            : IdentityPermissionSnapshot.Unavailable;
        var permissions = await new IdentityPermissionService(new StaticIdentityPermissionContext(snapshot)).GetPermissionsAsync();

        Assert.True(permissions.IsKnown);
        Assert.NotSame(UserPermissions.Unknown, permissions);
        Assert.False(permissions.Has("secrets", "view"));
        Assert.False(permissions.Has("identity/users", "delete"));
        Assert.Empty(permissions.Grants);
    }

    [Fact]
    public async Task DefaultMenuService_WithForbiddenSnapshot_HidesGatedItems()
    {
        var service = new DefaultMenuService(
            [new StaticMenuProvider(
                new() { Text = "Secrets", Href = "security/secrets", RequiredPermissions = { new("secrets", "view") } },
                new() { Text = "Dashboard", Href = "" })],
            [],
            new IdentityPermissionService(new StaticIdentityPermissionContext(IdentityPermissionSnapshot.Forbidden)));

        var items = (await service.GetMenuItemsAsync()).ToList();

        Assert.DoesNotContain(items, item => item.Text == "Secrets");
        Assert.Contains(items, item => item.Text == "Dashboard");
    }

    [Fact]
    public async Task DefaultMenuService_WithoutPermissionService_KeepsUngatedHostsUnfiltered()
    {
        var service = new DefaultMenuService(
            [new StaticMenuProvider(new Elsa.Studio.Models.MenuItem { Text = "Secrets", Href = "security/secrets", RequiredPermissions = { new("secrets", "view") } })],
            []);

        var items = (await service.GetMenuItemsAsync()).ToList();

        Assert.Contains(items, item => item.Text == "Secrets");
    }

    private sealed class StaticIdentityPermissionContext(IdentityPermissionSnapshot snapshot) : IIdentityPermissionContext
    {
        public Task<IdentityPermissionSnapshot> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(snapshot);

        public void Invalidate()
        {
        }
    }

    private sealed class StaticMenuProvider(params Elsa.Studio.Models.MenuItem[] items) : Elsa.Studio.Contracts.IMenuProvider
    {
        public ValueTask<IEnumerable<Elsa.Studio.Models.MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default) => new(items);
    }
}
