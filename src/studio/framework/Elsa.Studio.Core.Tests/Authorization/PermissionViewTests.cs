using Bunit;
using Elsa.Studio.Authorization;
using Elsa.Studio.Components;
using Elsa.Studio.Testing;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Core.Tests.Authorization;

public sealed class PermissionViewTests : BunitContext, IAsyncLifetime
{
    private const string Action = "create-secret";
    private const string Fallback = "no-access";

    private readonly StubPermissionService _permissions = new("secrets:view");
    private readonly NotifyingAuthenticationStateProvider _authentication = new();

    public PermissionViewTests()
    {
        Services.AddSingleton<IPermissionService>(_permissions);
        Services.AddSingleton<AuthenticationStateProvider>(_authentication);
    }

    [Fact]
    public void ThePageGrantsTheRequiredPermission_TheContentRendersWithoutResolvingPermissionsAgain()
    {
        var cut = RenderView(StubPermissionService.Grants("secrets:*"), PermissionVerbs.Write);

        Assert.Contains(Action, cut.Markup);
        Assert.Equal(0, _permissions.Calls);
    }

    [Fact]
    public void ThePageLacksTheRequiredPermission_TheDeniedContentRendersInstead()
    {
        var cut = RenderView(StubPermissionService.Grants("secrets:view"), PermissionVerbs.Write);

        Assert.DoesNotContain(Action, cut.Markup);
        Assert.Contains(Fallback, cut.Markup);
    }

    [Fact]
    public void PermissionsAreUnknown_TheContentRendersAsBefore()
    {
        var cut = RenderView(UserPermissions.Unknown, PermissionVerbs.Delete);

        Assert.Contains(Action, cut.Markup);
    }

    [Fact]
    public void EveryPermissionIsRequired()
    {
        Permission[] required = [new("secrets", PermissionVerbs.Write), new("secrets", PermissionVerbs.Delete)];

        var denied = RenderView(StubPermissionService.Grants("secrets:write"), allOf: required);
        var permitted = RenderView(StubPermissionService.Grants("secrets:write", "secrets:delete"), allOf: required);

        Assert.DoesNotContain(Action, denied.Markup);
        Assert.Contains(Action, permitted.Markup);
    }

    [Fact]
    public void OutsideAPage_ThePermissionsAreResolvedAndFollowAuthenticationChanges()
    {
        var cut = RenderView(pagePermissions: null, PermissionVerbs.Write);
        Assert.DoesNotContain(Action, cut.Markup);
        Assert.Equal(1, _permissions.Calls);

        _permissions.Permissions = StubPermissionService.Grants("secrets:write");
        _authentication.Notify();
        cut.WaitForAssertion(() => Assert.Contains(Action, cut.Markup));

        _permissions.Permissions = StubPermissionService.Grants("secrets:view");
        _authentication.Notify();
        cut.WaitForAssertion(() => Assert.DoesNotContain(Action, cut.Markup));
    }

    [Fact]
    public void OutsideAPage_AResolutionOvertakenByANewerOneIsIgnored()
    {
        var permissions = new PendingPermissionService();
        Services.AddSingleton<IPermissionService>(permissions);
        var cut = RenderView(pagePermissions: null, PermissionVerbs.Write);

        _authentication.Notify();
        cut.WaitForState(() => permissions.Pending == 2);
        permissions.Complete(1, StubPermissionService.Grants("secrets:write"));
        cut.WaitForAssertion(() => Assert.Contains(Action, cut.Markup));

        // Completing the first resolution lets initialization finish, which renders once more.
        var renders = cut.RenderCount;
        permissions.Complete(0, StubPermissionService.Grants("secrets:view"));
        cut.WaitForState(() => cut.RenderCount > renders);

        Assert.Contains(Action, cut.Markup);
    }

    [Fact]
    public async Task Disposed_StopsFollowingAuthenticationChanges()
    {
        RenderView(pagePermissions: null, PermissionVerbs.Write);

        await DisposeComponentsAsync();
        _authentication.Notify();

        Assert.Equal(1, _permissions.Calls);
    }

    [Fact]
    public void Wrap_GatesContentBuiltInCode()
    {
        var fragment = PermissionView.Wrap("secrets", PermissionVerbs.Write, builder => builder.AddContent(0, Action));

        var denied = Render(fragment);
        _permissions.Permissions = StubPermissionService.Grants("secrets:write");
        var permitted = Render(fragment);

        Assert.DoesNotContain(Action, denied.Markup);
        Assert.Contains(Action, permitted.Markup);
    }

    [Fact]
    public void AResourceWithoutAVerb_IsRejected() =>
        Assert.Throws<InvalidOperationException>(() => Render<PermissionView>(parameters => parameters
            .AddCascadingValue(UserPermissions.Unknown)
            .Add(x => x.Resource, "secrets")));

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    private IRenderedComponent<PermissionView> RenderView(UserPermissions? pagePermissions, string? verb = null, IEnumerable<Permission>? allOf = null) =>
        Render<PermissionView>(parameters =>
        {
            if (pagePermissions != null)
                parameters.AddCascadingValue(pagePermissions);

            if (verb != null)
                parameters.Add(x => x.Resource, "secrets").Add(x => x.Verb, verb);

            parameters
                .Add(x => x.AllOf, allOf)
                .Add(x => x.Denied, Fallback)
                .AddChildContent(Action);
        });

    /// <summary>Hands out resolutions that stay pending until the test completes them, in any order.</summary>
    private sealed class PendingPermissionService : IPermissionService
    {
        private readonly List<TaskCompletionSource<UserPermissions>> _resolutions = [];

        public ValueTask<UserPermissions> GetPermissionsAsync(CancellationToken cancellationToken = default)
        {
            var resolution = new TaskCompletionSource<UserPermissions>(TaskCreationOptions.RunContinuationsAsynchronously);
            _resolutions.Add(resolution);
            return new(resolution.Task);
        }

        public int Pending => _resolutions.Count;

        public void Complete(int index, UserPermissions permissions) => _resolutions[index].SetResult(permissions);
    }
}
