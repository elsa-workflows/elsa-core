using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Components;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using MudBlazor;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class RoleEditorSurfaceTests : BunitContext, IAsyncLifetime
{
    public RoleEditorSurfaceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void EditSurfaceShowsDirectCoveredUnverifiedAndRepairStates()
    {
        var roles = new StubRolesApi
        {
            Response = new ListRolesResponse
            {
                Roles =
                [
                    new RoleSummary
                    {
                        Id = "auditors",
                        Name = "Auditors",
                        Permissions = ["workflows/definitions:update", "workflows/*:view", "WorkflowDefinitions:Publish"]
                    }
                ]
            }
        };
        var permissions = new StubPermissionsApi
        {
            Response = new PermissionCatalogResponse
            {
                Resources =
                [
                    new PermissionResourceDescriptor
                    {
                        Resource = "workflows/definitions",
                        DisplayName = "Definitions",
                        Description = "Workflow definitions.",
                        Category = "Workflows",
                        SupportedVerbs = ["view", "update"],
                        Verified = false
                    }
                ]
            }
        };
        Register(roles, permissions);

        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.RoleId, "auditors")
            .Add(x => x.Access, ReadyAccess));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Edit role — Auditors", cut.Markup);
            Assert.Equal("Edit role — Auditors", cut.Find("h1").TextContent.Trim());
            Assert.Contains("Direct grant", cut.Markup);
            Assert.Contains("Covered by workflows/*:view", cut.Markup);
            Assert.Contains("Unverified · verified:false", cut.Markup);
            Assert.Contains("WorkflowDefinitions:Publish", cut.Markup);
            Assert.Contains("Save changes is disabled until issues are resolved", cut.Markup);
        });
    }

    [Fact]
    public void ExplicitReplacementPersistsAnExactGrantEvenWhenCoveredByWildcard()
    {
        var roles = new StubRolesApi
        {
            Response = new ListRolesResponse
            {
                Roles =
                [
                    new RoleSummary
                    {
                        Id = "auditors",
                        Name = "Auditors",
                        Permissions = ["workflows/*:view", "legacy:grant"]
                    }
                ]
            },
            Updated = new UpdateRoleResponse
            {
                Id = "auditors",
                Name = "Auditors",
                Permissions = ["workflows/*:view", "workflows/definitions:view"]
            }
        };
        var permissions = new StubPermissionsApi
        {
            Response = new PermissionCatalogResponse
            {
                Resources =
                [
                    new PermissionResourceDescriptor
                    {
                        Resource = "workflows/definitions",
                        DisplayName = "Definitions",
                        Category = "Workflows",
                        SupportedVerbs = ["view"]
                    }
                ]
            }
        };
        Register(roles, permissions);

        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.RoleId, "auditors")
            .Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("legacy:grant", cut.Markup));

        cut.Find("input[placeholder='resource:verb or wildcard']").Change("workflows/definitions:view");
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Replace").Click();
        cut.FindAll("button").Single(x => x.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, roles.UpdateCalls));
        Assert.Contains("workflows/definitions:view", roles.LastUpdate!.Permissions!);
    }

    [Fact]
    public async Task SaveIsSingleFlightWhileTheFirstRequestIsInProgress()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var roles = new StubRolesApi
        {
            Created = new CreateRoleResponse { Id = "new-role", Name = "New role" }
        };
        roles.CreateHandler = async cancellationToken =>
        {
            started.SetResult();
            await release.Task.WaitAsync(cancellationToken);
            return roles.Created;
        };
        Register(roles, new StubPermissionsApi());

        var cut = Render<RoleEditorSurface>(parameters => parameters.Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("New role", cut.Markup));
        cut.Find("input[aria-label='Role name']").Input("New role");
        var save = cut.FindAll("button").Single(x => x.TextContent.Contains("Create role", StringComparison.Ordinal));

        var first = save.ClickAsync();
        await Task.Yield();
        var currentSave = cut.FindAll("button").Single(x => x.TextContent.Contains("Saving", StringComparison.Ordinal));
        var second = currentSave.ClickAsync();
        await started.Task;
        release.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(1, roles.CreateCalls);
    }

    [Fact]
    public void ReachProviderFailureIsShownAsRecoverableGrantError()
    {
        var roles = new StubRolesApi();
        var permissions = new StubPermissionsApi();
        var provider = new StubBackendApiClientProvider(roles, permissions)
        {
            GetApiException = (type, count) => type == typeof(IPermissionsApi) && count > 1
                ? new InvalidOperationException("permissions provider unavailable")
                : null
        };
        Services.AddSingleton<IBackendApiClientProvider>(provider);
        Services.AddSingleton<IRoleDeletionService>(new StubRoleDeletionService());
        Render<MudPopoverProvider>();

        var cut = Render<RoleEditorSurface>(parameters => parameters.Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("New role", cut.Markup));
        cut.FindAll("[role='tab']").Single(x => x.TextContent.Contains("Advanced grants", StringComparison.OrdinalIgnoreCase)).Click();
        cut.Find("input[placeholder='workflows/*:view or *']").Change("workflows/*:view");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Add advanced grant", StringComparison.OrdinalIgnoreCase)).Click();

        Assert.Equal(2, provider.PermissionsApiCalls);
        cut.WaitForAssertion(() => Assert.Contains("Role administration is unavailable right now", cut.Markup));
    }

    [Fact]
    public void CreateSurfaceSendsNormalizedGrantsOnceAndNavigatesToCreatedRole()
    {
        var roles = new StubRolesApi
        {
            Created = new CreateRoleResponse { Id = "new-role", Name = "New role", Permissions = ["workflows/definitions:view"] }
        };
        var permissions = new StubPermissionsApi
        {
            Response = new PermissionCatalogResponse
            {
                Resources =
                [
                    new PermissionResourceDescriptor
                    {
                        Resource = "workflows/definitions",
                        DisplayName = "Definitions",
                        Category = "Workflows",
                        SupportedVerbs = ["view"]
                    }
                ]
            }
        };
        Register(roles, permissions);

        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("New role", cut.Markup));

        cut.Find("input[aria-label='Role name']").Input("  New role  ");
        cut.Find("input[aria-label='workflows/definitions:view']").Change(true);
        cut.FindAll("button").Single(x => x.TextContent.Contains("Create role", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, roles.CreateCalls));
        Assert.Equal("New role", roles.LastCreate!.Name);
        Assert.Equal(["workflows/definitions:view"], roles.LastCreate.Permissions);
        Assert.EndsWith("/security/roles/new-role", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void EditDeleteAction_OpensTheSameSharedDeletionDialog()
    {
        var roles = new StubRolesApi
        {
            Response = new ListRolesResponse
            {
                Roles = [new RoleSummary { Id = "auditors", Name = "Auditors" }]
            }
        };
        Register(roles, new StubPermissionsApi());
        var provider = Render<MudDialogProvider>();
        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.RoleId, "auditors")
            .Add(x => x.Access, ReadyAccess with { CanDelete = true }));
        cut.WaitForAssertion(() => Assert.Contains("Edit role — Auditors", cut.Markup));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Delete role", StringComparison.Ordinal)).Click();

        provider.WaitForAssertion(() =>
            Assert.Equal("auditors", provider.FindComponent<DeleteRoleDialog>().Instance.RoleId));
    }

    [Fact]
    public void SuccessfulAdvancedGrantClearsAnEarlierValidationError()
    {
        Register(new StubRolesApi(), new StubPermissionsApi());
        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("New role", cut.Markup));

        cut.FindAll("[role='tab']").Single(x => x.TextContent.Contains("Advanced grants", StringComparison.OrdinalIgnoreCase)).Click();
        cut.Find("input[placeholder='workflows/*:view or *']").Change("not-a-grant");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Add advanced grant", StringComparison.OrdinalIgnoreCase)).Click();
        Assert.Contains("Enter a valid grant", cut.Markup);

        cut.Find("input[placeholder='workflows/*:view or *']").Change("workflows/*:view");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Add advanced grant", StringComparison.OrdinalIgnoreCase)).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Enter a valid grant", cut.Markup);
            Assert.Contains("workflows/*:view", cut.Markup);
        });
    }

    [Fact]
    public void ExactPermissionSearch_MatchesTheFullPermissionIdentifier()
    {
        Register(new StubRolesApi(), new StubPermissionsApi
        {
            Response = new PermissionCatalogResponse
            {
                Resources =
                [
                    new PermissionResourceDescriptor
                    {
                        Resource = "workflows/definitions/labels",
                        DisplayName = "Definition labels",
                        Category = "Workflows",
                        SupportedVerbs = ["view"]
                    }
                ]
            }
        });

        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[aria-label='workflows/definitions/labels:view']")));

        cut.Find("input[placeholder='Search by name, ID, or permission']").Input("workflows/definitions/labels:view");

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[aria-label='workflows/definitions/labels:view']")));
    }

    private void Register(IRolesApi roles, IPermissionsApi permissions)
    {
        Services.AddSingleton<IBackendApiClientProvider>(new StubBackendApiClientProvider(roles, permissions));
        Services.AddSingleton<IRoleDeletionService>(new StubRoleDeletionService());
        Render<MudPopoverProvider>();
    }

    private static RoleAdministrationAccess ReadyAccess =>
        new(RoleAdministrationAccessState.Ready, CanView: true, CanCreate: true, CanUpdate: true, CanDelete: false);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await base.DisposeAsync();

    private sealed class StubBackendApiClientProvider(IRolesApi roles, IPermissionsApi permissions) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://localhost/");
        public Func<Type, int, Exception?>? GetApiException { get; init; }
        public int PermissionsApiCalls => _permissionsApiCalls;
        private int _permissionsApiCalls;

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            var count = typeof(T) == typeof(IPermissionsApi) ? ++_permissionsApiCalls : 0;
            var exception = GetApiException?.Invoke(typeof(T), count);
            if (exception is not null)
                return ValueTask.FromException<T>(exception);

            return ValueTask.FromResult(typeof(T) == typeof(IRolesApi) ? (T)(object)roles : (T)(object)permissions);
        }
    }

    private sealed class StubRolesApi : IRolesApi
    {
        public ListRolesResponse Response { get; set; } = new();
        public CreateRoleResponse Created { get; set; } = new();
        public int CreateCalls { get; private set; }
        public CreateRoleRequest? LastCreate { get; private set; }
        public int UpdateCalls { get; private set; }
        public UpdateRoleRequest? LastUpdate { get; private set; }
        public Func<CancellationToken, Task<CreateRoleResponse>>? CreateHandler { get; set; }
        public UpdateRoleResponse Updated { get; set; } = new();

        public Task<ListRolesResponse> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(Response);

        public Task<CreateRoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            LastCreate = request;
            return CreateHandler?.Invoke(cancellationToken) ?? Task.FromResult(Created);
        }

        public Task<UpdateRoleResponse> UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            LastUpdate = request;
            return Task.FromResult(Updated);
        }
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RoleDeletionImpactResponse> GetDeletionImpactAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemediateAndDeleteAsync(string id, RoleRemediationRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubPermissionsApi : IPermissionsApi
    {
        public PermissionCatalogResponse Response { get; set; } = new();

        public Task<PermissionCatalogResponse> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(Response);

        public Task<PermissionReachResponse> GetReachAsync(string resource, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PermissionReachResponse { Resource = resource, Covers = ["workflows/definitions"], Count = 1 });
    }

}
