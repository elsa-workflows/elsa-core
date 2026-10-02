using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Components;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class RolesPageTests : BunitContext, IAsyncLifetime
{
    private IRenderedComponent<MudPopoverProvider>? _popoverProvider;
    private IRenderedComponent<MudDialogProvider>? _dialogProvider;

    public RolesPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void Render_WhenRolesAreLoaded_ShowsTheNonPaginatedListWithoutTenantUi()
    {
        var api = new StubRolesApi
        {
            Response = new ListRolesResponse
            {
                Roles = [
                    new RoleSummary { Id = "administrators", Name = "Administrators", Permissions = ["*"] },
                    new RoleSummary { Id = "workflow-authors", Name = "Workflow Authors", Permissions = ["workflows/definitions:view"] }
                ]
            }
        };
        Register(api, ReadyAccess);

        var cut = RenderRoles();

        cut.WaitForAssertion(() => Assert.Contains("Administrators", cut.Markup));
        Assert.Equal("Roles", cut.Find("h1").TextContent.Trim());
        Assert.Contains("2 roles · all loaded", cut.Markup);
        Assert.Contains("Global access (*)", cut.Markup);
        Assert.DoesNotContain("Tenant", cut.Markup);
        Assert.DoesNotContain("mud-table-pagination", cut.Markup);
        var actionButtons = cut.FindAll("button[aria-label^='Actions for ']");
        Assert.Equal(4, actionButtons.Count);
        Assert.All(actionButtons, button => Assert.False(string.IsNullOrWhiteSpace(button.GetAttribute("aria-label"))));
    }

    [Fact]
    public void Search_WhenNameIdOrPermissionMatches_ShowsOnlyMatchingRoles()
    {
        var api = new StubRolesApi
        {
            Response = new ListRolesResponse
            {
                Roles = [
                    new RoleSummary { Id = "workflow-authors", Name = "Workflow Authors", Permissions = ["workflows/definitions:edit"] },
                    new RoleSummary { Id = "operations", Name = "Operations", Permissions = ["workflows/instances:retry"] }
                ]
            }
        };
        Register(api, ReadyAccess);

        var cut = RenderRoles();
        cut.WaitForAssertion(() => Assert.Contains("Workflow Authors", cut.Markup));

        var search = cut.Find("input[aria-label='Search roles by name, ID, or permission']");
        search.Input("retry");

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Operations", cut.Markup);
            Assert.DoesNotContain("Workflow Authors", cut.Markup);
        });
    }

    [Fact]
    public void Render_WhenCallerIsReadOnly_ShowsRolesButHidesMutationActions()
    {
        var api = new StubRolesApi
        {
            Response = new ListRolesResponse
            {
                Roles = [new RoleSummary { Id = "auditors", Name = "Auditors", Permissions = ["workflows/*:view"] }]
            }
        };
        Register(api, new RoleAdministrationAccess(RoleAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false));

        var cut = RenderRoles();

        cut.WaitForAssertion(() => Assert.Contains("Auditors", cut.Markup));
        Assert.Contains("You can view roles, but you cannot create, edit, or delete them.", cut.Markup);
        Assert.DoesNotContain("New role", cut.Markup);
        Assert.DoesNotContain("Edit", cut.Markup);
        Assert.DoesNotContain("Delete", cut.Markup);
    }

    [Fact]
    public void Render_WhenListIsLoading_ShowsTheLoadingState()
    {
        var rolesLoaded = new TaskCompletionSource<ListRolesResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = new StubRolesApi
        {
            ListHandler = (_, _) => rolesLoaded.Task
        };
        Register(api, ReadyAccess);

        var cut = RenderRoles();

        cut.WaitForAssertion(() => Assert.Contains("Loading roles...", cut.Markup));
        rolesLoaded.SetResult(new ListRolesResponse());
    }

    [Fact]
    public void Render_WhenListIsEmpty_ShowsCreateFirstRoleAction()
    {
        var api = new StubRolesApi { Response = new ListRolesResponse { Roles = [] } };
        Register(api, ReadyAccess);

        var cut = RenderRoles();

        cut.WaitForAssertion(() => Assert.Contains("No roles yet", cut.Markup));
        Assert.Contains("Create first role", cut.Markup);
    }

    [Fact]
    public void Render_WhenListFails_ShowsRetryAndLoadsRolesAfterRetry()
    {
        var api = new StubRolesApi
        {
            ListHandler = (_, call) => call == 1
                ? Task.FromException<ListRolesResponse>(new InvalidOperationException("connection failed"))
                : Task.FromResult(new ListRolesResponse { Roles = [new RoleSummary { Id = "ops", Name = "Operations" }] })
        };
        Register(api, ReadyAccess);

        var cut = RenderRoles();
        cut.WaitForAssertion(() => Assert.Contains("Roles could not be loaded", cut.Markup));

        cut.Find("button[aria-label='Try loading roles again']").Click();

        cut.WaitForAssertion(() => Assert.Contains("Operations", cut.Markup));
        Assert.Equal(2, api.ListCalls);
    }

    [Fact]
    public void CreateAndRowActions_NavigateToTheApprovedDetailsRoutes()
    {
        var api = new StubRolesApi
        {
            Response = new ListRolesResponse
            {
                Roles = [new RoleSummary { Id = "auditors/read only", Name = "Auditors" }]
            }
        };
        Register(api, ReadyAccess);
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = RenderRoles();
        cut.WaitForAssertion(() => Assert.Contains("Auditors", cut.Markup));

        cut.Find("button[aria-label='Create a new role']").Click();
        Assert.EndsWith("/security/roles/new", navigation.Uri, StringComparison.Ordinal);

        // A fresh render represents the list after the browser returns from the create route.
        var secondCut = Render<Roles>();
        secondCut.WaitForAssertion(() => Assert.Contains("Auditors", secondCut.Markup));
        secondCut.Find("tbody tr").Click();

        Assert.EndsWith("/security/roles/auditors%2Fread%20only", navigation.Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void DesktopRowsExposeAKeyboardOperableDetailsLink()
    {
        var api = new StubRolesApi
        {
            Response = new ListRolesResponse
            {
                Roles = [new RoleSummary { Id = "auditors", Name = "Auditors" }]
            }
        };
        Register(api, ReadyAccess);

        var cut = RenderRoles();
        cut.WaitForAssertion(() => Assert.Contains("Auditors", cut.Markup));

        var link = cut.Find("tbody tr a.role-row-link");
        Assert.Equal("/security/roles/auditors", link.GetAttribute("href"));
        Assert.Equal("Open Auditors role details", link.GetAttribute("aria-label"));
    }

    [Fact]
    public void DeleteAction_OpensTheSharedDeletionDialogWithoutNavigatingToDetails()
    {
        var api = new StubRolesApi
        {
            Response = new ListRolesResponse
            {
                Roles = [new RoleSummary { Id = "auditors", Name = "Auditors" }]
            }
        };
        Register(api, ReadyAccess);
        var navigation = Services.GetRequiredService<NavigationManager>();
        var originalUri = navigation.Uri;
        var cut = RenderRoles();
        cut.WaitForAssertion(() => Assert.Contains("Auditors", cut.Markup));

        var popoverProvider = _popoverProvider!;
        var dialogProvider = _dialogProvider!;
        cut.FindAll(".mud-menu button").First().Click();
        popoverProvider.WaitForAssertion(() =>
            popoverProvider.FindAll(".mud-menu-item").Single(x => x.TextContent.Trim() == "Delete").Click());

        dialogProvider.WaitForAssertion(() =>
            Assert.Equal("auditors", dialogProvider.FindComponent<DeleteRoleDialog>().Instance.RoleId));
        Assert.Equal(originalUri, navigation.Uri);
    }

    private void Register(IRolesApi api, RoleAdministrationAccess access)
    {
        Services.AddSingleton<IRoleAdministrationAccessService>(new TestRoleAccessService(access));
        Services.AddSingleton<IBackendApiClientProvider>(new TestBackendApiClientProvider(api));
        Services.AddSingleton<IRoleDeletionService>(new StubRoleDeletionService());
    }

    private IRenderedComponent<Roles> RenderRoles()
    {
        _popoverProvider ??= Render<MudPopoverProvider>();
        _dialogProvider ??= Render<MudDialogProvider>();
        return Render<Roles>();
    }

    private static RoleAdministrationAccess ReadyAccess =>
        new(RoleAdministrationAccessState.Ready, CanView: true, CanCreate: true, CanUpdate: true, CanDelete: true);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await base.DisposeAsync();

    private sealed class TestRoleAccessService(RoleAdministrationAccess access) : IRoleAdministrationAccessService
    {
        public Task<RoleAdministrationAccess> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(access);

        public void Invalidate()
        {
        }
    }

    private sealed class TestBackendApiClientProvider(IRolesApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://localhost/");

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class =>
            ValueTask.FromResult((T)(object)api);
    }

    private sealed class StubRolesApi : IRolesApi
    {
        public ListRolesResponse Response { get; set; } = new();
        public Func<CancellationToken, int, Task<ListRolesResponse>>? ListHandler { get; set; }
        public int ListCalls { get; private set; }

        public Task<ListRolesResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            ListCalls++;
            return ListHandler?.Invoke(cancellationToken, ListCalls) ?? Task.FromResult(Response);
        }

        public Task<CreateRoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<UpdateRoleResponse> UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<RoleDeletionImpactResponse> GetDeletionImpactAsync(string id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task RemediateAndDeleteAsync(string id, RoleRemediationRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

}
