using System.Net;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Constants;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Refit;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class IdentityPermissionContextTests
{
    [Fact]
    public async Task GetAsync_WhenMePermissionsReturnsForbidden_ReturnsForbiddenSnapshot()
    {
        var api = new TestMePermissionsApi(_ => Task.FromException<CurrentCallerPermissionsResponse>(CreateApiException(HttpStatusCode.Forbidden)));
        var context = CreateContext(api);

        var snapshot = await context.GetAsync();

        Assert.Equal(IdentityPermissionSnapshotState.Forbidden, snapshot.State);
        Assert.False(snapshot.HasPermission(IdentityPermissions.RolesResource, IdentityPermissions.View));
    }

    [Fact]
    public async Task GetAsync_WhenMePermissionsIsUnavailable_ReturnsUnavailableSnapshot()
    {
        var api = new TestMePermissionsApi(_ => Task.FromException<CurrentCallerPermissionsResponse>(new HttpRequestException("Identity is unavailable.")));
        var context = CreateContext(api);

        var snapshot = await context.GetAsync();

        Assert.Equal(IdentityPermissionSnapshotState.Unavailable, snapshot.State);
        Assert.False(snapshot.HasPermission(IdentityPermissions.RolesResource, IdentityPermissions.View));
    }

    [Fact]
    public async Task GetAsync_MergesDuplicateResourcesOrdinallyAndKeepsCaseDistinct()
    {
        var api = new TestMePermissionsApi(_ => Task.FromResult(new CurrentCallerPermissionsResponse
        {
            Grants =
            [
                new CurrentCallerResourceGrant { Resource = IdentityPermissions.RolesResource, Verbs = [IdentityPermissions.View, IdentityPermissions.Create] },
                new CurrentCallerResourceGrant { Resource = IdentityPermissions.RolesResource, Verbs = [IdentityPermissions.Update] },
                new CurrentCallerResourceGrant { Resource = "Identity/Roles", Verbs = [IdentityPermissions.Delete] }
            ]
        }));
        var context = CreateContext(api);

        var snapshot = await context.GetAsync();

        Assert.Equal(IdentityPermissionSnapshotState.Ready, snapshot.State);
        Assert.True(snapshot.HasPermission(IdentityPermissions.RolesResource, IdentityPermissions.View));
        Assert.True(snapshot.HasPermission(IdentityPermissions.RolesResource, IdentityPermissions.Create));
        Assert.True(snapshot.HasPermission(IdentityPermissions.RolesResource, IdentityPermissions.Update));
        Assert.False(snapshot.HasPermission(IdentityPermissions.RolesResource, IdentityPermissions.Delete));
        Assert.True(snapshot.HasPermission("Identity/Roles", IdentityPermissions.Delete));
    }

    [Fact]
    public async Task GetAsync_CachesTheSnapshotUntilInvalidated()
    {
        var calls = 0;
        var api = new TestMePermissionsApi(_ =>
        {
            calls++;
            return Task.FromResult(new CurrentCallerPermissionsResponse
            {
                Grants = [new CurrentCallerResourceGrant { Resource = IdentityPermissions.RolesResource, Verbs = [IdentityPermissions.View] }]
            });
        });
        var context = CreateContext(api);

        var first = await context.GetAsync();
        var cached = await context.GetAsync();
        context.Invalidate();
        var refreshed = await context.GetAsync();

        Assert.Same(first, cached);
        Assert.NotSame(cached, refreshed);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task GetAsync_PropagatesCancellationFromTheMePermissionsCall()
    {
        using var cancellation = new CancellationTokenSource();
        var api = new TestMePermissionsApi(async token =>
        {
            cancellation.Cancel();
            await Task.Yield();
            token.ThrowIfCancellationRequested();
            return new CurrentCallerPermissionsResponse();
        });
        var context = CreateContext(api);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.GetAsync(cancellation.Token));
    }

    private static ApiException CreateApiException(HttpStatusCode statusCode) =>
        ApiException.Create(
            new HttpRequestMessage(HttpMethod.Get, "https://elsa.example/identity/me/permissions"),
            HttpMethod.Get,
            new HttpResponseMessage(statusCode),
            new RefitSettings()).GetAwaiter().GetResult();

    private static IdentityPermissionContext CreateContext(IMePermissionsApi api) =>
        new(new StaticBackendApiClientProvider(api), NullLogger<IdentityPermissionContext>.Instance);
}

public sealed class RoleAdministrationAccessServiceTests
{
    [Fact]
    public async Task GetAsync_WhenRemoteFeatureIsDisabled_FailsClosedAsUnavailable()
    {
        var permissions = new TestPermissionContext(_ => throw new InvalidOperationException("Must not load permissions."));
        var service = new RoleAdministrationAccessService(new TestRemoteFeatureProvider(false), permissions);

        var access = await service.GetAsync();

        Assert.Equal(RoleAdministrationAccessState.Unavailable, access.State);
        Assert.False(access.CanView);
        Assert.Equal(0, permissions.Calls);
    }

    [Fact]
    public async Task GetAsync_WhenRemoteFeatureCheckFails_FailsClosedAsUnavailable()
    {
        var service = new RoleAdministrationAccessService(
            new TestRemoteFeatureProvider(new HttpRequestException("Core is unavailable.")),
            new TestPermissionContext(_ => Task.FromResult(IdentityPermissionSnapshot.Forbidden)));

        var access = await service.GetAsync();

        Assert.Equal(RoleAdministrationAccessState.Unavailable, access.State);
        Assert.False(access.CanView);
    }

    [Fact]
    public async Task GetAsync_RequiresTheViewGrantBeforeExposingDirectAccess()
    {
        var snapshot = new IdentityPermissionSnapshot(
            IdentityPermissionSnapshotState.Ready,
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                [IdentityPermissions.RolesResource] = new HashSet<string>([IdentityPermissions.Create], StringComparer.Ordinal)
            });
        var service = new RoleAdministrationAccessService(new TestRemoteFeatureProvider(true), new TestPermissionContext(_ => Task.FromResult(snapshot)));

        var access = await service.GetAsync();

        Assert.Equal(RoleAdministrationAccessState.Forbidden, access.State);
        Assert.False(access.CanView);
        Assert.False(access.CanCreate);
        Assert.False(access.CanUpdate);
        Assert.False(access.CanDelete);
    }

    [Fact]
    public async Task GetAsync_ReturnsReadOnlyCapabilitiesWhenOnlyViewIsGranted()
    {
        var service = CreateService(IdentityPermissions.View);

        var access = await service.GetAsync();

        Assert.Equal(RoleAdministrationAccessState.Ready, access.State);
        Assert.True(access.CanView);
        Assert.False(access.CanCreate);
        Assert.False(access.CanUpdate);
        Assert.False(access.CanDelete);
    }

    [Theory]
    [InlineData(IdentityPermissions.Create, true, false, false)]
    [InlineData(IdentityPermissions.Update, false, true, false)]
    [InlineData(IdentityPermissions.Delete, false, false, true)]
    public async Task GetAsync_MapsCreateUpdateAndDeleteIndependently(string mutation, bool canCreate, bool canUpdate, bool canDelete)
    {
        var service = CreateService(IdentityPermissions.View, mutation);

        var access = await service.GetAsync();

        Assert.Equal(RoleAdministrationAccessState.Ready, access.State);
        Assert.True(access.CanView);
        Assert.Equal(canCreate, access.CanCreate);
        Assert.Equal(canUpdate, access.CanUpdate);
        Assert.Equal(canDelete, access.CanDelete);
    }

    private static RoleAdministrationAccessService CreateService(params string[] verbs)
    {
        var snapshot = new IdentityPermissionSnapshot(
            IdentityPermissionSnapshotState.Ready,
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                [IdentityPermissions.RolesResource] = new HashSet<string>(verbs, StringComparer.Ordinal)
            });
        return new RoleAdministrationAccessService(new TestRemoteFeatureProvider(true), new TestPermissionContext(_ => Task.FromResult(snapshot)));
    }
}

internal sealed class TestMePermissionsApi(Func<CancellationToken, Task<CurrentCallerPermissionsResponse>> handler) : IMePermissionsApi
{
    public Task<CurrentCallerPermissionsResponse> GetAsync(CancellationToken cancellationToken = default) => handler(cancellationToken);
}

internal sealed class TestPermissionContext(Func<CancellationToken, Task<IdentityPermissionSnapshot>> handler) : IIdentityPermissionContext
{
    public int Calls { get; private set; }
    public int Invalidations { get; private set; }

    public Task<IdentityPermissionSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        Calls++;
        return handler(cancellationToken);
    }

    public void Invalidate() => Invalidations++;
}

internal sealed class TestRemoteFeatureProvider : IRemoteFeatureProvider
{
    private readonly bool? _enabled;
    private readonly Exception? _exception;

    public TestRemoteFeatureProvider(bool enabled)
    {
        _enabled = enabled;
    }

    public TestRemoteFeatureProvider(Exception exception)
    {
        _exception = exception;
    }

    public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        if (_exception != null)
            throw _exception;

        return Task.FromResult(_enabled == true && featureName == Feature.RemoteFeatureName);
    }

    public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
}

public sealed class UserAdministrationAccessServiceTests
{
    [Fact]
    public async Task GetAsync_WhenRemoteFeatureIsDisabled_FailsClosedAsUnavailable()
    {
        var permissions = new TestPermissionContext(_ => throw new InvalidOperationException("Must not load permissions."));
        var service = new UserAdministrationAccessService(new TestRemoteFeatureProvider(false), permissions);

        var access = await service.GetAsync();

        Assert.Equal(UserAdministrationAccessState.Unavailable, access.State);
        Assert.False(access.CanView);
        Assert.Equal(0, permissions.Calls);
    }

    [Theory]
    [InlineData(IdentityPermissionSnapshotState.Unavailable, UserAdministrationAccessState.Unavailable)]
    [InlineData(IdentityPermissionSnapshotState.Forbidden, UserAdministrationAccessState.Forbidden)]
    public async Task GetAsync_WhenThePermissionSnapshotIsNotReady_FailsClosed(IdentityPermissionSnapshotState snapshotState, UserAdministrationAccessState expected)
    {
        var snapshot = snapshotState == IdentityPermissionSnapshotState.Forbidden ? IdentityPermissionSnapshot.Forbidden : IdentityPermissionSnapshot.Unavailable;
        var service = new UserAdministrationAccessService(new TestRemoteFeatureProvider(true), new TestPermissionContext(_ => Task.FromResult(snapshot)));

        var access = await service.GetAsync();

        Assert.Equal(expected, access.State);
        Assert.False(access.CanView);
        Assert.False(access.CanCreate);
        Assert.False(access.CanUpdate);
        Assert.False(access.CanDelete);
    }

    [Fact]
    public async Task GetAsync_RequiresTheUsersViewGrantAndIgnoresRoleGrants()
    {
        var snapshot = new IdentityPermissionSnapshot(
            IdentityPermissionSnapshotState.Ready,
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                [IdentityPermissions.UsersResource] = new HashSet<string>([IdentityPermissions.Create, IdentityPermissions.Delete], StringComparer.Ordinal),
                [IdentityPermissions.RolesResource] = new HashSet<string>([IdentityPermissions.View], StringComparer.Ordinal)
            });
        var service = new UserAdministrationAccessService(new TestRemoteFeatureProvider(true), new TestPermissionContext(_ => Task.FromResult(snapshot)));

        var access = await service.GetAsync();

        Assert.Equal(UserAdministrationAccessState.Forbidden, access.State);
        Assert.False(access.CanView);
        Assert.False(access.CanCreate);
        Assert.False(access.CanDelete);
    }

    [Fact]
    public async Task GetAsync_ReturnsReadOnlyCapabilitiesWhenOnlyViewIsGranted()
    {
        var access = await CreateService(IdentityPermissions.View).GetAsync();

        Assert.Equal(UserAdministrationAccessState.Ready, access.State);
        Assert.True(access.CanView);
        Assert.False(access.CanCreate);
        Assert.False(access.CanUpdate);
        Assert.False(access.CanDelete);
    }

    [Theory]
    [InlineData(IdentityPermissions.Create, true, false, false)]
    [InlineData(IdentityPermissions.Update, false, true, false)]
    [InlineData(IdentityPermissions.Delete, false, false, true)]
    public async Task GetAsync_MapsCreateUpdateAndDeleteIndependently(string mutation, bool canCreate, bool canUpdate, bool canDelete)
    {
        var access = await CreateService(IdentityPermissions.View, mutation).GetAsync();

        Assert.Equal(UserAdministrationAccessState.Ready, access.State);
        Assert.True(access.CanView);
        Assert.Equal(canCreate, access.CanCreate);
        Assert.Equal(canUpdate, access.CanUpdate);
        Assert.Equal(canDelete, access.CanDelete);
    }

    [Fact]
    public void Invalidate_ClearsTheSharedPermissionSnapshot()
    {
        var permissions = new TestPermissionContext(_ => Task.FromResult(IdentityPermissionSnapshot.Forbidden));
        var service = new UserAdministrationAccessService(new TestRemoteFeatureProvider(true), permissions);

        service.Invalidate();

        Assert.Equal(1, permissions.Invalidations);
    }

    private static UserAdministrationAccessService CreateService(params string[] verbs)
    {
        var snapshot = new IdentityPermissionSnapshot(
            IdentityPermissionSnapshotState.Ready,
            new Dictionary<string, IReadOnlySet<string>>(StringComparer.Ordinal)
            {
                [IdentityPermissions.UsersResource] = new HashSet<string>(verbs, StringComparer.Ordinal)
            });
        return new UserAdministrationAccessService(new TestRemoteFeatureProvider(true), new TestPermissionContext(_ => Task.FromResult(snapshot)));
    }
}
