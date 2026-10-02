using Bunit;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class DeleteRoleDialogTests : BunitContext, IAsyncLifetime
{
    private static readonly RoleAdministrationAccess CanDelete = new(
        RoleAdministrationAccessState.Ready,
        CanView: true,
        CanCreate: false,
        CanUpdate: false,
        CanDelete: true);

    public DeleteRoleDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public async Task Render_WhenCallerCannotDelete_FailsClosedWithoutShowingMutationControls()
    {
        var service = new FakeRoleDeletionService();
        Services.AddSingleton<IRoleDeletionService>(service);

        var cut = await ShowAsync("role-1", "Administrators", RoleAdministrationAccess.Forbidden);
        var dialog = cut.FindComponent<DeleteRoleDialog>();

        Assert.Contains("not allowed to delete this role", dialog.Markup, StringComparison.Ordinal);
        Assert.Equal(0, service.InspectCalls);
        Assert.DoesNotContain("Delete Administrators", dialog.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Render_WhenImpactIsSafe_ShowsSafeConfirmationAndDeleteAction()
    {
        Services.AddSingleton<IRoleDeletionService>(new FakeRoleDeletionService
        {
            Inspection = new RoleDeletionInspectionResult
            {
                Outcome = RoleDeletionInspectionOutcome.Safe,
                Impact = new RoleDeletionImpactResponse
                {
                    RoleId = "role-1",
                    DependencyVersion = "dep-1",
                    CanDelete = true
                }
            }
        });

        var cut = await ShowAsync("role-1", "Auditors", CanDelete);
        var dialog = cut.FindComponent<DeleteRoleDialog>();

        Assert.Contains("Safe to delete", dialog.Markup, StringComparison.Ordinal);
        Assert.Contains("Delete Auditors", dialog.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DeleteAction_IsDoubleSubmitSafe()
    {
        var deletionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowDeletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var service = new FakeRoleDeletionService
        {
            Inspection = SafeInspection(),
            DeleteHandler = async cancellationToken =>
            {
                deletionStarted.SetResult();
                await allowDeletion.Task.WaitAsync(cancellationToken);
                return new RoleDeletionOperationResult { Outcome = RoleDeletionOperationOutcome.Deleted };
            }
        };
        Services.AddSingleton<IRoleDeletionService>(service);
        var cut = await ShowAsync("role-1", "Auditors", CanDelete);
        var dialog = cut.FindComponent<DeleteRoleDialog>();

        var deleteButton = dialog.FindAll("button").Single(x => x.TextContent.Contains("Delete Auditors", StringComparison.Ordinal));
        var first = deleteButton.ClickAsync();
        await deletionStarted.Task;
        await dialog.InvokeAsync(() => dialog.FindAll("button").Single(x => x.TextContent.Contains("Delete Auditors", StringComparison.Ordinal)).Click());
        allowDeletion.SetResult();
        await first;

        Assert.Equal(1, service.DeleteCalls);
    }

    [Fact]
    public async Task DependencyConflict_ShowsFreshImpactAndDoesNotAutomaticallyRetry()
    {
        var service = new FakeRoleDeletionService
        {
            Inspection = SafeInspection("dep-1"),
            DeleteHandler = _ => Task.FromResult(new RoleDeletionOperationResult
            {
                Outcome = RoleDeletionOperationOutcome.DependencyConflict,
                Code = "role_dependency_changed",
                Impact = new RoleDeletionImpactResponse
                {
                    RoleId = "role-1",
                    DependencyVersion = "dep-2",
                    EditableReferences = [new RoleDeletionDependencyResponse { OwnerKey = "Contractor SSO" }]
                }
            })
        };
        Services.AddSingleton<IRoleDeletionService>(service);
        var cut = await ShowAsync("role-1", "Auditors", CanDelete);
        var dialog = cut.FindComponent<DeleteRoleDialog>();

        await dialog.FindAll("button").Single(x => x.TextContent.Contains("Delete Auditors", StringComparison.Ordinal)).ClickAsync();

        Assert.Contains("Impact changed", dialog.Markup, StringComparison.Ordinal);
        Assert.Contains("Review refreshed impact", dialog.Markup, StringComparison.Ordinal);
        Assert.Equal(1, service.InspectCalls);
        Assert.Equal(1, service.DeleteCalls);
    }

    [Fact]
    public async Task IncompleteRemediation_ShowsChangedAndRemainingOwnersAndKeepsRoleRetained()
    {
        var service = new FakeRoleDeletionService
        {
            Inspection = new RoleDeletionInspectionResult
            {
                Outcome = RoleDeletionInspectionOutcome.RemediationRequired,
                Impact = new RoleDeletionImpactResponse
                {
                    RoleId = "role-1",
                    DependencyVersion = "dep-1",
                    ExecutionMode = "bestEffort",
                    CanDelete = false,
                    CanRemediate = true,
                    EditableReferences = [new RoleDeletionDependencyResponse { OwnerKey = "Employee SSO" }]
                }
            },
            RemediationHandler = _ => Task.FromResult(new RoleDeletionOperationResult
            {
                Outcome = RoleDeletionOperationOutcome.Incomplete,
                Code = "role_remediation_incomplete",
                ChangedOwnerIds = ["partner-sso"],
                Impact = new RoleDeletionImpactResponse
                {
                    RoleId = "role-1",
                    DependencyVersion = "dep-2",
                    EditableReferences = [new RoleDeletionDependencyResponse { OwnerKey = "Employee SSO" }]
                }
            })
        };
        Services.AddSingleton<IRoleDeletionService>(service);
        var (cut, reference) = await OpenAsync("role-1", "Operations", CanDelete);
        var dialog = cut.FindComponent<DeleteRoleDialog>();

        var checkboxes = dialog.FindAll("input[type=checkbox]");
        foreach (var checkbox in checkboxes)
            await checkbox.ChangeAsync(true);
        await dialog.FindAll("button").Single(x => x.TextContent.Contains("Apply remediation", StringComparison.Ordinal)).ClickAsync();

        Assert.Contains("Role retained", dialog.Markup, StringComparison.Ordinal);
        Assert.Contains("partner-sso", dialog.Markup, StringComparison.Ordinal);
        Assert.Contains("Employee SSO", dialog.Markup, StringComparison.Ordinal);

        dialog.FindAll("button").Single(x => x.TextContent.Trim() == "Close").Click();
        var result = await reference.Result;
        var deletionResult = Assert.IsType<DeleteRoleDialogResult>(result!.Data);
        Assert.True(deletionResult.WasRetained);
        Assert.True(deletionResult.ShouldRefresh);
        Assert.Equal(["partner-sso"], deletionResult.ChangedOwnerIds);
    }

    [Fact]
    public async Task Remediation_SendsSelectedReferencesAndReplacementDefaultRole()
    {
        RoleDeletionConfirmation? submitted = null;
        var service = new FakeRoleDeletionService
        {
            Inspection = new RoleDeletionInspectionResult
            {
                Outcome = RoleDeletionInspectionOutcome.RemediationRequired,
                Impact = new RoleDeletionImpactResponse
                {
                    RoleId = "role-1",
                    DependencyVersion = "dep-1",
                    CanDelete = false,
                    CanRemediate = true,
                    Warnings = ["removes_last_default_role"],
                    EditableReferences =
                    [
                        new RoleDeletionDependencyResponse { Source = "external-authentication", OwnerId = "connection-a", OwnerKey = "Connection A" },
                        new RoleDeletionDependencyResponse { Source = "external-authentication", OwnerId = "connection-b", OwnerKey = "Connection B" }
                    ]
                }
            },
            RemediationHandler = confirmation =>
            {
                submitted = confirmation;
                return Task.FromResult(new RoleDeletionOperationResult { Outcome = RoleDeletionOperationOutcome.Deleted });
            }
        };
        var (provider, _) = await OpenAsync(
            "role-1",
            "Auditors",
            CanDelete,
            [new RoleSummary { Id = "replacement", Name = "Workflow Authors" }],
            service);
        var dialog = provider.FindComponent<DeleteRoleDialog>();

        var selects = dialog.FindComponents<MudSelect<string>>();
        Assert.Contains("Replacement default role", dialog.Markup, StringComparison.Ordinal);
        await dialog.InvokeAsync(() => selects.Single().Instance.ValueChanged.InvokeAsync("replacement"));

        var checkboxes = dialog.FindAll("input[type=checkbox]");
        Assert.Equal(4, checkboxes.Count);
        await checkboxes[1].ChangeAsync(false);
        await checkboxes[2].ChangeAsync(true);
        await checkboxes[3].ChangeAsync(true);
        await dialog.FindAll("button").Single(x => x.TextContent.Contains("Apply remediation", StringComparison.Ordinal)).ClickAsync();

        Assert.NotNull(submitted);
        Assert.Equal("replacement", submitted!.ReplacementDefaultRoleId);
        Assert.Equal(
            [new RoleDeletionReferenceSelection { Source = "external-authentication", OwnerId = "connection-a" }],
            submitted.SelectedReferences);
    }

    [Fact]
    public async Task Remediation_OnlyRequiresReplacementForSelectedFinalDefaultReference()
    {
        var service = new FakeRoleDeletionService
        {
            Inspection = new RoleDeletionInspectionResult
            {
                Outcome = RoleDeletionInspectionOutcome.RemediationRequired,
                Impact = new RoleDeletionImpactResponse
                {
                    RoleId = "role-1",
                    DependencyVersion = "dep-1",
                    CanDelete = false,
                    CanRemediate = true,
                    Warnings = ["removes_last_default_role"],
                    EditableReferences =
                    [
                        new RoleDeletionDependencyResponse { Source = "external-authentication", OwnerId = "final", RemovesLastDefaultRole = true },
                        new RoleDeletionDependencyResponse { Source = "external-authentication", OwnerId = "other" }
                    ]
                }
            }
        };
        var (provider, _) = await OpenAsync("role-1", "Auditors", CanDelete, service: service);
        var dialog = provider.FindComponent<DeleteRoleDialog>();

        Assert.Contains("Replacement default role", dialog.Markup, StringComparison.Ordinal);
        await dialog.FindAll("input[type=checkbox]")[0].ChangeAsync(false);

        Assert.DoesNotContain("Replacement default role", dialog.Markup, StringComparison.Ordinal);
    }

    private async Task<IRenderedComponent<MudDialogProvider>> ShowAsync(
        string roleId,
        string roleName,
        RoleAdministrationAccess access)
    {
        var (provider, _) = await OpenAsync(roleId, roleName, access);
        return provider;
    }

    private async Task<(IRenderedComponent<MudDialogProvider> Provider, IDialogReference Reference)> OpenAsync(
        string roleId,
        string roleName,
        RoleAdministrationAccess access,
        IReadOnlyCollection<RoleSummary>? replacementRoles = null,
        FakeRoleDeletionService? service = null)
    {
        if (service is not null)
            Services.AddSingleton<IRoleDeletionService>(service);
        Render<MudPopoverProvider>();
        var provider = Render<MudDialogProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            [nameof(DeleteRoleDialog.RoleId)] = roleId,
            [nameof(DeleteRoleDialog.RoleName)] = roleName,
            [nameof(DeleteRoleDialog.Access)] = access,
            [nameof(DeleteRoleDialog.ReplacementRoles)] = replacementRoles ?? []
        };
        var reference = await dialogService.ShowAsync<DeleteRoleDialog>("Delete role", parameters);
        provider.WaitForAssertion(() => provider.FindComponent<DeleteRoleDialog>());
        return (provider, reference);
    }

    private static RoleDeletionInspectionResult SafeInspection(string version = "dep-1") => new()
    {
        Outcome = RoleDeletionInspectionOutcome.Safe,
        Impact = new RoleDeletionImpactResponse
        {
            RoleId = "role-1",
            DependencyVersion = version,
            CanDelete = true
        }
    };

    private sealed class FakeRoleDeletionService : IRoleDeletionService
    {
        public RoleDeletionInspectionResult Inspection { get; init; } = SafeInspection();
        public Func<CancellationToken, Task<RoleDeletionOperationResult>>? DeleteHandler { get; init; }
        public Func<RoleDeletionConfirmation, Task<RoleDeletionOperationResult>>? RemediationHandler { get; init; }
        public int InspectCalls { get; private set; }
        public int DeleteCalls { get; private set; }

        public Task<RoleDeletionInspectionResult> InspectAsync(string roleId, RoleAdministrationAccess access, CancellationToken cancellationToken = default)
        {
            InspectCalls++;
            return Task.FromResult(Inspection);
        }

        public Task<RoleDeletionOperationResult> DeleteAsync(string roleId, RoleAdministrationAccess access, CancellationToken cancellationToken = default)
        {
            DeleteCalls++;
            return DeleteHandler?.Invoke(cancellationToken) ?? Task.FromResult(new RoleDeletionOperationResult { Outcome = RoleDeletionOperationOutcome.Deleted });
        }

        public Task<RoleDeletionOperationResult> RemediateAndDeleteAsync(string roleId, RoleAdministrationAccess access, RoleDeletionConfirmation confirmation, CancellationToken cancellationToken = default) =>
            RemediationHandler?.Invoke(confirmation) ?? Task.FromResult(new RoleDeletionOperationResult { Outcome = RoleDeletionOperationOutcome.Deleted });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await base.DisposeAsync();
}
