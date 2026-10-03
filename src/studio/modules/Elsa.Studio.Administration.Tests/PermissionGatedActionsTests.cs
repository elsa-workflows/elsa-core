using System.Diagnostics.CodeAnalysis;
using Bunit;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Authorization;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Components;
using Elsa.Studio.Labels.Contracts;
using Elsa.Studio.Labels.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Secrets.Client;
using Elsa.Studio.Secrets.Components;
using Elsa.Studio.Secrets.Models;
using Elsa.Studio.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using LabelPage = Elsa.Studio.Labels.UI.Pages.Label;
using LabelsPage = Elsa.Studio.Labels.UI.Pages.Labels;
using SecretPage = Elsa.Studio.Secrets.Pages.Secret;
using SecretsPage = Elsa.Studio.Secrets.Pages.Secrets;

namespace Elsa.Studio.Administration.Tests;

/// <summary>A user who can only view a module's records does not get its write actions.</summary>
public sealed class PermissionGatedActionsTests : BunitContext, IAsyncLifetime
{
    private static readonly SecretModel ApiKey = new() { Id = "1", Name = "api-key", DisplayName = "API key", TypeName = "Text", StoreName = "Database" };
    private static readonly Label Urgent = new() { Id = "urgent", Name = "Urgent" };

    public PermissionGatedActionsTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer>(new TestLocalizer());
        Services.AddSingleton<IBackendApiClientProvider>(new ApiProvider());
        Services.AddSingleton<IWorkflowDefinitionLabelsProvider>(new DefinitionLabelsProvider());
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void SecretsList_ViewOnly_HidesCreateAndRowActions()
    {
        var cut = RenderPage<SecretsPage>(["secrets:view"]);

        cut.WaitForAssertion(() => Assert.Contains(ApiKey.DisplayName, cut.Markup));
        Assert.DoesNotContain("Create Secret", cut.Markup);
        Assert.Empty(cut.FindAll("tbody .mud-menu"));
    }

    [Fact]
    public void SecretsList_WithWriteAccess_ShowsCreateAndRowActions()
    {
        var cut = RenderPage<SecretsPage>(["secrets:*"]);

        cut.WaitForAssertion(() => Assert.Contains(ApiKey.DisplayName, cut.Markup));
        Assert.Contains("Create Secret", cut.Markup);
        Assert.Single(cut.FindAll("tbody .mud-menu"));
    }

    [Fact]
    public void Secret_ViewOnly_HidesEditingRevocationAndRotation()
    {
        var cut = RenderPage<SecretPage>(["secrets:view"], parameters => parameters.Add(x => x.Name, ApiKey.Name));

        cut.WaitForAssertion(() => Assert.Contains("Secret details", cut.Markup));
        Assert.DoesNotContain("Edit details", cut.Markup);
        Assert.DoesNotContain("Revoke", cut.Markup);
        Assert.DoesNotContain("Rotation", cut.Markup);
    }

    [Fact]
    public void Secret_WithWriteAccess_ShowsEditingRevocationAndRotation()
    {
        var cut = RenderPage<SecretPage>(["secrets:view", "secrets:write"], parameters => parameters.Add(x => x.Name, ApiKey.Name));

        cut.WaitForAssertion(() => Assert.Contains("Edit details", cut.Markup));
        Assert.Contains("Revoke", cut.Markup);
        Assert.Contains("Rotation", cut.Markup);
    }

    [Fact]
    public void LabelsList_ViewOnly_HidesCreateAndBulkActions()
    {
        var cut = RenderPage<LabelsPage>(["labels:view"]);

        cut.WaitForAssertion(() => Assert.Contains(Urgent.Name!, cut.Markup));
        Assert.DoesNotContain("Create Label", cut.Markup);
        Assert.DoesNotContain("Bulk actions", cut.Markup);
    }

    [Fact]
    public void LabelsList_WithCreateAndDeleteAccess_ShowsCreateAndBulkActions()
    {
        var cut = RenderPage<LabelsPage>(["labels:view", "labels:create", "labels:delete"]);

        cut.WaitForAssertion(() => Assert.Contains(Urgent.Name!, cut.Markup));
        Assert.Contains("Create Label", cut.Markup);
        Assert.Contains("Bulk actions", cut.Markup);
    }

    [Fact]
    public void Label_ViewOnly_HidesSave()
    {
        var cut = RenderPage<LabelPage>(["labels:view"], parameters => parameters.Add(x => x.LabelId, Urgent.Id));

        cut.WaitForAssertion(() => Assert.Contains($"Label: {Urgent.Name}", cut.Markup));
        Assert.DoesNotContain("Save", cut.Markup);
    }

    [Fact]
    public void Label_WithUpdateAccess_ShowsSave()
    {
        var cut = RenderPage<LabelPage>(["labels:view", "labels:update"], parameters => parameters.Add(x => x.LabelId, Urgent.Id));

        cut.WaitForAssertion(() => Assert.Contains("Save", cut.Markup));
    }

    [Theory]
    [InlineData(new[] { "secrets:view" }, false)]
    [InlineData(new[] { "secrets:view", "secrets:write" }, true)]
    public void SecretPicker_OffersInlineCreateOnlyWithWriteAccess(string[] grants, bool offered)
    {
        var cut = RenderPage<SecretPicker>(grants);

        Assert.Equal(offered, cut.FindComponents<MudIconButton>().Any(x => x.Instance.Icon == Icons.Material.Filled.Add));
    }

    [Theory]
    [InlineData(new[] { "workflows/definitions/labels:view" }, false, false)]
    [InlineData(new[] { "workflows/definitions/labels:view", "workflows/definitions/labels:update" }, true, false)]
    [InlineData(new[] { "workflows/definitions/labels:view", "workflows/definitions/labels:update", "labels:view" }, true, true)]
    public void DefinitionLabelsEditor_OffersRemovingAndAddingPerPermission(string[] grants, bool canRemove, bool canAdd)
    {
        var cut = RenderPage<WorkflowDefinitionLabelsEditor>(grants, parameters => parameters.Add(x => x.WorkflowDefinition, new WorkflowDefinition { DefinitionId = "definition-1" }));

        cut.WaitForAssertion(() => Assert.Contains(Urgent.Name!, cut.Markup));
        Assert.Equal(canRemove, cut.FindAll(".mud-chip-close-button").Any());
        Assert.Equal(canAdd, cut.Markup.Contains("Add Label"));
    }

    // The shell's page guard cascades the user's permissions to the page.
    private IRenderedComponent<TPage> RenderPage<TPage>(string[] grants, Action<ComponentParameterCollectionBuilder<TPage>>? parameters = null) where TPage : IComponent =>
        Render<TPage>(builder =>
        {
            builder.AddCascadingValue(StubPermissionService.Grants(grants));
            parameters?.Invoke(builder);
        });

    private sealed class ApiProvider : IBackendApiClientProvider
    {
        private readonly object[] _apis = [new SecretsApi(), new LabelsApi()];

        public Uri Url => new("https://elsa.example.test");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class =>
            new(_apis.OfType<T>().Single());
    }

    private sealed class SecretsApi : ISecretsApi
    {
        public Task<ListSecretsResponse> ListAsync(string? search = null, string? typeName = null, string? storeName = null, string? scope = null, SecretStatus? status = null, int? page = null, int? pageSize = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ListSecretsResponse { Items = [ApiKey], TotalCount = 1 });

        public Task<SecretModel> GetAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(ApiKey);
        public Task<SecretDescriptorsResponse> GetDescriptorsAsync(CancellationToken cancellationToken = default) => Task.FromResult(new SecretDescriptorsResponse());
        public Task<SecretModel> CreateAsync(CreateSecretRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SecretModel> UpdateAsync(string name, UpdateSecretRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SecretModel> RotateAsync(string name, RotateSecretRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SecretModel> RevokeAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SecretTestResponse> TestAsync(string name, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SecretPickerResponse> PickAsync(SecretPickerRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(new SecretPickerResponse { Items = [ApiKey], CanCreateInline = true });
    }

    private sealed class DefinitionLabelsProvider : IWorkflowDefinitionLabelsProvider
    {
        public Task<IEnumerable<WorkflowDefinitionLabelDescriptor>> ListAsync(string workflowDefinitionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<WorkflowDefinitionLabelDescriptor>>([new() { Id = Urgent.Id, Name = Urgent.Name }]);

        public Task<IEnumerable<WorkflowDefinitionLabelDescriptor>> UpdateAsync(string workflowDefinitionId, IEnumerable<string> selectedLabelsIds, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class LabelsApi : ILabelsApi
    {
        public Task<ListResponse<Label>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(new ListResponse<Label>([Urgent], 1));
        public Task<Label> GetAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(Urgent);
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(string id, LabelInputModel model, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Label> CreateAsync(LabelInputModel? inputModel, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
