using System.Diagnostics.CodeAnalysis;
using System.Net;
using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Components;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Refit;
using Xunit;
using UserEditor = Elsa.Studio.Security.Pages.User;
using UsersPage = Elsa.Studio.Security.Pages.Users;

namespace Elsa.Studio.Administration.Tests;

public sealed class IdentityManagementTests : BunitContext, IAsyncLifetime
{
    private static readonly UserAdministrationAccess FullUserAccess =
        new(UserAdministrationAccessState.Ready, CanView: true, CanCreate: true, CanUpdate: true, CanDelete: true);

    private static readonly UserAdministrationAccess ViewOnlyUserAccess =
        new(UserAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false);

    private static readonly RoleAdministrationAccess ViewOnlyRoleAccess =
        new(RoleAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false);

    private readonly UsersApi _users = new();
    private readonly RolesApi _roles = new();
    private readonly StubUserAccessService _userAccess = new(FullUserAccess);
    private readonly StubRoleAccessService _roleAccess = new(ViewOnlyRoleAccess);
    private readonly Clipboard _clipboard = new();
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public IdentityManagementTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IBackendApiClientProvider>(new ApiProvider(_users, _roles));
        Services.AddSingleton<IUserAdministrationAccessService>(_userAccess);
        Services.AddSingleton<IRoleAdministrationAccessService>(_roleAccess);
        Services.AddSingleton<IClipboard>(_clipboard);
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void UserList_RendersRolesTenantScopeAndFiltersLocally()
    {
        _users.Users =
        [
            new() { Id = "user-1", Name = "alice", Roles = ["admin"], TenantId = null },
            new() { Id = "user-2", Name = "bob", Roles = ["operator"], TenantId = "tenant-a" }
        ];

        var cut = Render<UsersPage>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("alice", cut.Markup);
            Assert.Contains("operator", cut.Markup);
            Assert.Contains("tenant-a", cut.Markup);
            Assert.Contains("Host", cut.Markup);
            Assert.Contains("2 users · all loaded", cut.Markup);
            Assert.Contains("Mixed tenant scopes", cut.Markup);
            Assert.Contains("Open user alice", cut.Markup);
            Assert.Equal(Breakpoint.Md, cut.FindComponent<MudTable<UserSummary>>().Instance.Breakpoint);
        });

        cut.Find("input[placeholder='Search users']").Input("operator");

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("alice", cut.Markup);
            Assert.Contains("bob", cut.Markup);
            Assert.Contains("1 user · matching search", cut.Markup);
        });
    }

    [Fact]
    public void UserList_ShowsTheTenantScopeCoreReturnedWithoutFilteringClientSide()
    {
        _users.Users =
        [
            new() { Id = "user-1", Name = "alice", TenantId = "tenant-a" },
            new() { Id = "user-2", Name = "bob", TenantId = "tenant-a" }
        ];

        var cut = Render<UsersPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Tenant tenant-a", cut.Markup);
            Assert.Equal(2, cut.FindAll("tbody tr").Count);
        });
        Assert.Equal(1, _users.ListCallCount);
    }

    [Fact]
    public async Task UserList_PresentsUserIdSeparatelyAndCopiesTheFullValue()
    {
        _users.Users = [new() { Id = "user-with-a-long-id", Name = "alice", Roles = ["admin"] }];

        var cut = Render<UsersPage>();
        cut.WaitForAssertion(() =>
        {
            var row = cut.Find("tbody tr");
            Assert.Contains("User ID", cut.Markup);
            Assert.Equal("alice", row.QuerySelector("td[data-label='Name']")?.TextContent.Trim());
            Assert.Equal("user-with-a-long-id", row.QuerySelector("td[data-label='User ID'] .identity-id-value")?.TextContent.Trim());
            Assert.DoesNotContain("user-with-a-long-id", row.QuerySelector("td[data-label='Name']")?.TextContent);
        });

        await cut.InvokeAsync(() => cut.Find("button[aria-label='Copy user ID user-with-a-long-id']").Click());

        Assert.Equal("user-with-a-long-id", _clipboard.LastCopiedText);
    }

    [Fact]
    public void UserList_WhenAccessIsForbidden_DeniesDirectNavigationWithoutCallingCore()
    {
        _userAccess.Access = UserAdministrationAccess.Forbidden;
        _users.Users = [new() { Id = "user-1", Name = "alice" }];

        var cut = Render<UsersPage>();

        cut.WaitForAssertion(() => Assert.Contains("User administration access is required", cut.Markup));
        Assert.DoesNotContain("alice", cut.Markup);
        Assert.Equal(0, _users.ListCallCount);
    }

    [Fact]
    public void UserList_WhenAccessIsUnavailable_FailsClosed()
    {
        _userAccess.Access = UserAdministrationAccess.Unavailable;

        var cut = Render<UsersPage>();

        cut.WaitForAssertion(() => Assert.Contains("User administration is unavailable", cut.Markup));
        Assert.Equal(0, _users.ListCallCount);
    }

    [Fact]
    public void UserList_WhenLoadingFails_OffersRetry()
    {
        var calls = 0;
        _users.ListHandler = () => ++calls == 1
            ? Task.FromException<ListUsersResponse>(new HttpRequestException("offline"))
            : Task.FromResult(new ListUsersResponse { Users = [new() { Id = "user-1", Name = "alice" }] });

        var cut = Render<UsersPage>();
        cut.WaitForAssertion(() => Assert.Contains("Users could not be loaded", cut.Markup));

        cut.Find("button[aria-label='Try loading users again']").Click();

        cut.WaitForAssertion(() => Assert.Contains("alice", cut.Markup));
    }

    [Fact]
    public void UserEditor_WhenAccessIsForbidden_DeniesDirectNavigationWithoutCallingCore()
    {
        _userAccess.Access = UserAdministrationAccess.Forbidden;
        _users.Users = [new() { Id = "user-1", Name = "alice" }];

        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-1"));

        cut.WaitForAssertion(() => Assert.Contains("User administration access is required", cut.Markup));
        Assert.DoesNotContain("alice", cut.Markup);
        Assert.Equal(0, _users.ListCallCount);
        Assert.Equal(0, _roles.ListCallCount);
    }

    [Fact]
    public void UserEditor_WhenCallerCannotCreate_ExplainsInsteadOfRenderingTheForm()
    {
        _userAccess.Access = ViewOnlyUserAccess;

        var cut = Render<UserEditor>();

        cut.WaitForAssertion(() => Assert.Contains("cannot create a user", cut.Markup));
        Assert.Contains("identity/users:create", cut.Markup);
        Assert.Empty(cut.FindAll("button").Where(x => x.TextContent.Trim() == "Create user"));
        Assert.Equal(0, _roles.ListCallCount);
    }

    [Fact]
    public void ReadOnlyLists_UseViewAffordancesAndHideMutations()
    {
        _userAccess.Access = ViewOnlyUserAccess;
        _users.Users = [new() { Id = "user-1", Name = "alice" }];

        var users = Render<UsersPage>();

        users.WaitForAssertion(() =>
        {
            Assert.Contains("You can view users, but you cannot create, edit, or delete them.", users.Markup);
            Assert.Contains("View user alice", users.Markup);
            Assert.DoesNotContain("Edit user alice", users.Markup);
            Assert.DoesNotContain("Create user", users.Markup);
            Assert.DoesNotContain("Delete user alice", users.Markup);
        });
    }

    [Theory]
    [InlineData(true, false, false, "Create user", "Edit user alice", "Delete user alice")]
    [InlineData(false, true, false, "Edit user alice", "Create user", "Delete user alice")]
    [InlineData(false, false, true, "Delete user alice", "Create user", "Edit user alice")]
    public void UserList_GatesEachMutationIndependently(bool canCreate, bool canUpdate, bool canDelete, string present, string absentFirst, string absentSecond)
    {
        _userAccess.Access = new UserAdministrationAccess(UserAdministrationAccessState.Ready, CanView: true, canCreate, canUpdate, canDelete);
        _users.Users = [new() { Id = "user-1", Name = "alice" }];

        var cut = Render<UsersPage>();

        cut.WaitForAssertion(() => Assert.Contains(present, cut.Markup));
        Assert.DoesNotContain(absentFirst, cut.Markup);
        Assert.DoesNotContain(absentSecond, cut.Markup);
        Assert.DoesNotContain("You can view users, but you cannot create, edit, or delete them.", cut.Markup);
    }

    [Fact]
    public void ReadOnlyEditor_DisablesFieldsAndHidesSaveAndDelete()
    {
        _userAccess.Access = ViewOnlyUserAccess;
        _users.Users = [new() { Id = "user-1", Name = "alice", Roles = ["admin-role"] }];
        _roles.Roles = [new() { Id = "admin-role", Name = "Administrators" }];

        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-1"));

        cut.WaitForAssertion(() => Assert.Contains("You can view this user, but your current sign-in cannot edit it.", cut.Markup));
        Assert.NotNull(cut.Find("input[readonly][disabled][value='alice']"));
        Assert.Empty(cut.FindAll("button").Where(x => x.TextContent.Trim() is "Save changes" or "Delete user"));
        Assert.All(cut.FindAll("input[type='password']"), input => Assert.True(input.HasAttribute("disabled")));
    }

    [Fact]
    public async Task CreateUser_UsesRoleIdsAndShowsGeneratedPasswordOnce()
    {
        _roles.Roles = [new() { Id = "power-user", Name = "Power User" }];
        _users.CreateResult = new() { Id = "user-3", Name = "carol", GeneratedPassword = "once-only", Roles = [] };
        var cut = Render<UserEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Create an Elsa account", cut.Markup));

        await cut.InvokeAsync(() => cut.FindComponent<MudSelect<string>>().Instance.SelectedValuesChanged.InvokeAsync(["power-user"]));
        cut.Find("input[type='text']").Change("carol");
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Create user").Click();

        _dialogProvider.WaitForAssertion(() =>
        {
            Assert.Equal("carol", _users.CreateRequest?.Name);
            Assert.Null(_users.CreateRequest?.Password);
            Assert.Equal(["power-user"], _users.CreateRequest?.Roles);
            Assert.Contains("once-only", _dialogProvider.Markup);
            Assert.Contains("shown once", _dialogProvider.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("cannot be retrieved later", _dialogProvider.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task CreateUser_WithSuppliedPassword_NeverShowsAPasswordDialogEvenIfCoreEchoesOne()
    {
        _users.CreateResult = new() { Id = "user-3", Name = "carol", GeneratedPassword = "supplied-password" };
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<UserEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Create an Elsa account", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("input[type='text']").Change("carol"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[0].Change("supplied-password"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[1].Change("supplied-password"));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Create user").Click());

        cut.WaitForAssertion(() => Assert.EndsWith("/security/users/user-3", navigation.Uri, StringComparison.Ordinal));
        Assert.Equal("supplied-password", _users.CreateRequest?.Password);
        Assert.DoesNotContain("supplied-password", _dialogProvider.Markup);
        Assert.DoesNotContain("Generated password", _dialogProvider.Markup);
    }

    [Fact]
    public async Task RoleSelector_RequiresRoleViewPermissionAndKeepsExistingAssignments()
    {
        _roleAccess.Access = RoleAdministrationAccess.Forbidden;
        _users.Users = [new() { Id = "user-1", Name = "alice", Roles = ["admin-role"] }];
        _roles.Roles = [new() { Id = "admin-role", Name = "Administrators" }];
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-1"));
        cut.WaitForAssertion(() => Assert.Contains("alice", cut.Markup));

        Assert.Empty(cut.FindComponents<MudSelect<string>>());
        Assert.Contains("Role assignment is unavailable", cut.Markup);
        Assert.Contains("identity/roles:view", cut.Markup);
        Assert.Contains("admin-role", cut.Markup);
        Assert.Equal(0, _roles.ListCallCount);

        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[0].Change("replacement-password"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[1].Change("replacement-password"));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Save changes").Click());

        cut.WaitForAssertion(() => Assert.Equal("replacement-password", _users.UpdateRequest?.Password));
        Assert.Null(_users.UpdateRequest?.Roles);
    }

    [Fact]
    public async Task UpdateUser_WhenCoreRefusesTheRoleAssignment_ExplainsTheForbiddenRoles()
    {
        _users.Users = [new() { Id = "user-1", Name = "alice", Roles = [] }];
        _roles.Roles = [new() { Id = "admin-role", Name = "Administrators" }];
        _users.UpdateHandler = (_, _) => Task.FromException<UserSummary>(CreateApiException(HttpStatusCode.Forbidden));
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-1"));
        cut.WaitForAssertion(() => Assert.Contains("alice", cut.Markup));

        await cut.InvokeAsync(() => cut.FindComponent<MudSelect<string>>().Instance.SelectedValuesChanged.InvokeAsync(["admin-role"]));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Save changes").Click());

        cut.WaitForAssertion(() => Assert.Contains(UserEditorSurface.RoleAssignmentForbiddenMessage, cut.Markup));
        Assert.Equal(["admin-role"], _users.UpdateRequest?.Roles);
        Assert.NotNull(cut.FindAll("button").Single(x => x.TextContent.Trim() == "Save changes"));
    }

    [Fact]
    public async Task CreateUser_WhenCoreRefusesTheRoleAssignment_ExplainsTheForbiddenRoles()
    {
        _roles.Roles = [new() { Id = "admin-role", Name = "Administrators" }];
        _users.CreateHandler = _ => Task.FromException<CreateUserResponse>(CreateApiException(HttpStatusCode.Forbidden));
        var cut = Render<UserEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Create an Elsa account", cut.Markup));

        await cut.InvokeAsync(() => cut.FindComponent<MudSelect<string>>().Instance.SelectedValuesChanged.InvokeAsync(["admin-role"]));
        await cut.InvokeAsync(() => cut.Find("input[type='text']").Change("carol"));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Create user").Click());

        cut.WaitForAssertion(() => Assert.Contains(UserEditorSurface.RoleAssignmentForbiddenMessage, cut.Markup));
    }

    [Fact]
    public async Task UpdateUser_WhenCoreForbidsWithoutRoleChanges_ShowsTheGenericAuthorizationMessage()
    {
        _users.Users = [new() { Id = "user-1", Name = "alice", Roles = [] }];
        _users.UpdateHandler = (_, _) => Task.FromException<UserSummary>(CreateApiException(HttpStatusCode.Forbidden));
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-1"));
        cut.WaitForAssertion(() => Assert.Contains("alice", cut.Markup));

        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[0].Change("replacement-password"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[1].Change("replacement-password"));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Save changes").Click());

        cut.WaitForAssertion(() => Assert.Contains("You are not allowed to perform this user administration action.", cut.Markup));
        Assert.DoesNotContain(UserEditorSurface.RoleAssignmentForbiddenMessage, cut.Markup);
    }

    [Fact]
    public async Task PasswordOnlyUserUpdate_DoesNotResubmitUnchangedRoles()
    {
        _users.Users = [new() { Id = "user-1", Name = "alice", Roles = ["admin-role"] }];
        _roles.Roles = [new() { Id = "admin-role", Name = "Administrators" }];
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-1"));
        cut.WaitForAssertion(() => Assert.Contains("alice", cut.Markup));

        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[0].Change("replacement-password"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[1].Change("replacement-password"));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Save changes").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("user-1", _users.UpdateId);
            Assert.Equal("replacement-password", _users.UpdateRequest?.Password);
            Assert.Null(_users.UpdateRequest?.Roles);
        });
    }

    [Fact]
    public async Task UserEditor_ReloadsAndClearsPasswordsWhenRouteIdChangesAfterCreate()
    {
        _users.CreateResult = new() { Id = "user-3", Name = "server-carol", Roles = [] };
        _users.Users = [new() { Id = "user-3", Name = "server-carol", Roles = [], TenantId = "tenant-a" }];
        var cut = Render<UserEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Create an Elsa account", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("input[type='text']").Change("carol"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[0].Change("supplied-password"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[1].Change("supplied-password"));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Create user").Click());
        cut.WaitForAssertion(() => Assert.Equal("carol", _users.CreateRequest?.Name));

        cut.Render(parameters => parameters.Add(component => component.Id, "user-3"));

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("input[readonly][value='server-carol']"));
            Assert.Contains("Scope: tenant-a", cut.Markup);
            Assert.Contains("Copy user ID user-3", cut.Markup);
            Assert.All(cut.FindAll("input[type='password']"), input => Assert.True(string.IsNullOrEmpty(input.GetAttribute("value"))));
        });
    }

    [Fact]
    public void UserEditor_IgnoresAStaleLoadAfterTheRouteChanges()
    {
        var firstLoad = new TaskCompletionSource<ListUsersResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondLoad = new TaskCompletionSource<ListUsersResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _users.ListResults.Enqueue(firstLoad.Task);
        _users.ListResults.Enqueue(secondLoad.Task);
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-a"));
        cut.WaitForState(() => _users.ListCallCount == 1);

        cut.Render(parameters => parameters.Add(component => component.Id, "user-b"));
        cut.WaitForState(() => _users.ListCallCount == 2);
        secondLoad.SetResult(new() { Users = [new() { Id = "user-b", Name = "bob" }] });
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("input[readonly][value='bob']")));

        firstLoad.SetResult(new() { Users = [new() { Id = "user-a", Name = "alice" }] });
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("input[readonly][value='bob']"));
            Assert.DoesNotContain("alice", cut.Markup);
        });
    }

    [Fact]
    public void UserDeletion_RequiresConfirmation()
    {
        _users.Users = [new() { Id = "user-1", Name = "alice" }];
        var cut = Render<UsersPage>();
        cut.WaitForAssertion(() => Assert.Contains("Delete user alice", cut.Markup));

        cut.Find("button[aria-label='Delete user alice']").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Delete user?", _dialogProvider.Markup));
        _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Cancel").Click();

        Assert.Empty(_users.DeletedIds);
    }

    [Fact]
    public async Task UserListDeletion_DisablesTheRowActionWhileTheRequestIsPending()
    {
        var deleteCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _users.DeleteHandler = _ => deleteCompletion.Task;
        _users.Users = [new() { Id = "user-1", Name = "alice" }];
        var cut = Render<UsersPage>();
        cut.WaitForAssertion(() => Assert.Contains("Delete user alice", cut.Markup));

        cut.Find("button[aria-label='Delete user alice']").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Delete user?", _dialogProvider.Markup));
        var confirmTask = _dialogProvider.InvokeAsync(() =>
            _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Delete").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, _users.DeleteCallCount);
            Assert.True(cut.Find("button[aria-label='Delete user alice']").HasAttribute("disabled"));
        });
        cut.Find("button[aria-label='Delete user alice']").Click();
        Assert.Equal(1, _users.DeleteCallCount);

        deleteCompletion.SetResult();
        await confirmTask;
        cut.WaitForAssertion(() => Assert.DoesNotContain("alice", cut.Markup));
    }

    [Fact]
    public async Task UserListDeletion_WhenCoreReportsAConflict_KeepsTheUserAndExplainsTheDependency()
    {
        const string conflictMessage = "The user is referenced by one or more installed modules.";
        _users.DeleteHandler = _ => Task.FromException(CreateApiException(HttpStatusCode.Conflict,
            $$$"""{ "error": "conflict", "message": "{{{conflictMessage}}}", "dependencies": [{ "source": "external-authentication" }] }"""));
        _users.Users = [new() { Id = "user-1", Name = "alice" }];
        var cut = Render<UsersPage>();
        cut.WaitForAssertion(() => Assert.Contains("Delete user alice", cut.Markup));

        cut.Find("button[aria-label='Delete user alice']").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Delete user?", _dialogProvider.Markup));
        await _dialogProvider.InvokeAsync(() => _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Delete").Click());

        cut.WaitForAssertion(() => Assert.Contains(conflictMessage, cut.Markup));
        Assert.Contains("alice", cut.Markup);
        Assert.DoesNotContain("external-authentication", cut.Markup);
        Assert.False(cut.Find("button[aria-label='Delete user alice']").HasAttribute("disabled"));
    }

    [Fact]
    public async Task UserEditorDeletion_WhenCoreReportsAConflict_ShowsTheConflictInTheDangerZone()
    {
        const string conflictMessage = "The user is referenced by one or more installed modules.";
        _users.DeleteHandler = _ => Task.FromException(CreateApiException(HttpStatusCode.Conflict,
            $$$"""{ "error": "conflict", "message": "{{{conflictMessage}}}" }"""));
        _users.Users = [new() { Id = "user-a", Name = "alice" }];
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-a"));
        cut.WaitForAssertion(() => Assert.Contains("alice", cut.Markup));
        var uriBeforeDelete = navigation.Uri;

        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Delete user").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Delete alice?", _dialogProvider.Markup));
        await _dialogProvider.InvokeAsync(() => _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Delete").Click());

        cut.WaitForAssertion(() => Assert.Contains(conflictMessage, cut.Find(".user-danger-zone").TextContent));
        Assert.Equal(uriBeforeDelete, navigation.Uri);
        Assert.False(cut.FindAll("button").Single(x => x.TextContent.Trim() == "Delete user").HasAttribute("disabled"));
    }

    [Fact]
    public void UserDeletion_IsCancelledWhenTheRouteChangesDuringConfirmation()
    {
        _users.Users =
        [
            new() { Id = "user-a", Name = "alice" },
            new() { Id = "user-b", Name = "bob" }
        ];
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-a"));
        cut.WaitForAssertion(() => Assert.Contains("alice", cut.Markup));

        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Delete user").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Delete alice?", _dialogProvider.Markup));
        cut.Render(parameters => parameters.Add(component => component.Id, "user-b"));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("input[readonly][value='bob']")));
        _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Delete").Click();

        Assert.Empty(_users.DeletedIds);
    }

    [Fact]
    public async Task UserDeletion_DisablesTheEditorWhileTheRequestIsPending()
    {
        var deleteCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _users.DeleteHandler = _ => deleteCompletion.Task;
        _users.Users = [new() { Id = "user-a", Name = "alice" }];
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-a"));
        cut.WaitForAssertion(() => Assert.Contains("alice", cut.Markup));

        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Delete user").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Delete alice?", _dialogProvider.Markup));
        var confirmTask = _dialogProvider.InvokeAsync(() =>
            _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Delete").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, _users.DeleteCallCount);
            Assert.True(cut.FindAll("button").Single(x => x.TextContent.Trim() == "Delete user").HasAttribute("disabled"));
            Assert.True(cut.FindAll("button").Single(x => x.TextContent.Trim() == "Save changes").HasAttribute("disabled"));
        });
        deleteCompletion.SetResult();
        await confirmTask;
        Assert.Equal(["user-a"], _users.DeletedIds);
    }

    [Fact]
    public async Task GeneratedPasswordCompletion_DoesNotNavigateAfterEditorDisposal()
    {
        _users.CreateResult = new() { Id = "user-3", Name = "carol", GeneratedPassword = "once-only" };
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<UserEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Create an Elsa account", cut.Markup));
        await cut.InvokeAsync(() => cut.Find("input[type='text']").Change("carol"));
        var createTask = cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Create user").Click());
        _dialogProvider.WaitForAssertion(() => Assert.Contains("once-only", _dialogProvider.Markup));
        var uriBeforeDisposal = navigation.Uri;

        await cut.FindComponent<UserEditorSurface>().Instance.DisposeAsync();
        await _dialogProvider.InvokeAsync(() =>
            _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "I have saved it").Click());
        await createTask;
        cut.Dispose();

        Assert.Equal(uriBeforeDisposal, navigation.Uri);
    }

    private static ApiException CreateApiException(HttpStatusCode statusCode, string content = "")
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://elsa.example/identity/users");
        using var response = new HttpResponseMessage(statusCode) { Content = new StringContent(content) };
        return ApiException.Create(request, HttpMethod.Post, response, new RefitSettings()).GetAwaiter().GetResult();
    }

    private sealed class ApiProvider(UsersApi users, RolesApi roles) : IBackendApiClientProvider
    {
        public Uri Url => new("https://elsa.example.test");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            object api = typeof(T) == typeof(IUsersApi) ? users : roles;
            return new((T)api);
        }
    }

    private sealed class StubUserAccessService(UserAdministrationAccess access) : IUserAdministrationAccessService
    {
        public UserAdministrationAccess Access { get; set; } = access;
        public Task<UserAdministrationAccess> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Access);
        public void Invalidate()
        {
        }
    }

    private sealed class StubRoleAccessService(RoleAdministrationAccess access) : IRoleAdministrationAccessService
    {
        public RoleAdministrationAccess Access { get; set; } = access;
        public Task<RoleAdministrationAccess> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Access);
        public void Invalidate()
        {
        }
    }

    private sealed class Clipboard : IClipboard
    {
        public string? LastCopiedText { get; private set; }

        public Task CopyText(string text, CancellationToken cancellationToken = default)
        {
            LastCopiedText = text;
            return Task.CompletedTask;
        }
    }

    private sealed class UsersApi : IUsersApi
    {
        public ICollection<UserSummary> Users { get; set; } = [];
        public Queue<Task<ListUsersResponse>> ListResults { get; } = [];
        public Func<Task<ListUsersResponse>>? ListHandler { get; set; }
        public int ListCallCount { get; private set; }
        public CreateUserRequest? CreateRequest { get; private set; }
        public CreateUserResponse CreateResult { get; set; } = new();
        public Func<CreateUserRequest, Task<CreateUserResponse>>? CreateHandler { get; set; }
        public string? UpdateId { get; private set; }
        public UpdateUserRequest? UpdateRequest { get; private set; }
        public Func<string, UpdateUserRequest, Task<UserSummary>>? UpdateHandler { get; set; }
        public ICollection<string> DeletedIds { get; } = [];
        public int DeleteCallCount { get; private set; }
        public Func<string, Task>? DeleteHandler { get; set; }

        public Task<ListUsersResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            ListCallCount++;
            if (ListResults.Count > 0)
                return ListResults.Dequeue();
            return ListHandler?.Invoke() ?? Task.FromResult(new ListUsersResponse { Users = Users });
        }

        public Task<CreateUserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            CreateRequest = request;
            return CreateHandler?.Invoke(request) ?? Task.FromResult(CreateResult);
        }

        public Task<UserSummary> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            UpdateId = id;
            UpdateRequest = request;
            return UpdateHandler?.Invoke(id, request) ?? Task.FromResult(Users.Single(user => user.Id == id));
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;
            DeletedIds.Add(id);
            return DeleteHandler?.Invoke(id) ?? Task.CompletedTask;
        }
    }

    private sealed class RolesApi : IRolesApi
    {
        public ICollection<RoleSummary> Roles { get; set; } = [];
        public int ListCallCount { get; private set; }

        public Task<ListRolesResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            ListCallCount++;
            return Task.FromResult(new ListRolesResponse { Roles = Roles });
        }

        public Task<CreateRoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UpdateRoleResponse> UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RoleDeletionImpactResponse> GetDeletionImpactAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemediateAndDeleteAsync(string id, RoleRemediationRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
