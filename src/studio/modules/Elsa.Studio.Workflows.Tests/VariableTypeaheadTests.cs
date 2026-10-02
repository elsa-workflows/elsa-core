using Bunit;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.Variables.Components;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Extensions;
using MudBlazor.Services;
using MudExtensions;
using MudExtensions.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class VariableTypeaheadTests : BunitContext, IAsyncLifetime
{
    public VariableTypeaheadTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddMudExtensions();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IVariableTypeService, VariableTypeServiceStub>();
        Services.AddSingleton<IStorageDriverService, StorageDriverServiceStub>();
        Services.AddSingleton<IIdentityGenerator, IdentityGeneratorStub>();
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Theory]
    [InlineData("s", "System.String")]
    [InlineData("S", "System.String")]
    [InlineData("i", "System.Int32")]
    [InlineData("ss", "System.Security.SecureString")]
    [InlineData("sss", "System.String")]
    [InlineData("z", "System.Boolean")]
    public async Task TypeaheadSelectsByDisplayName(string keys, string expectedTypeName)
    {
        var provider = Render<MudDialogProvider>();
        await provider.InvokeAsync(() => Services.GetRequiredService<IDialogService>().ShowAsync<EditVariableDialog>("Add variable",
            new DialogParameters<EditVariableDialog> { { x => x.WorkflowDefinition, new WorkflowDefinition() } }));
        var select = provider.FindComponent<MudSelectExtended<VariableTypeDescriptor>>();

        await select.InvokeAsync(() => select.Instance.OpenMenu());
        var interceptor = Services.GetRequiredService<IKeyInterceptorService>();
        var elementId = select.Find(".mud-select-extended").Id;
        // Dispatch through the same callback as JavaScript; bUnit does not run key interception JS.
        var onKeyDown = interceptor.GetType().GetMethod("OnKeyDown")!;
        foreach (var pressedKey in keys.Select(x => x.ToString()).Append("Enter"))
            await select.InvokeAsync(() => (Task)onKeyDown.Invoke(interceptor, [elementId, new KeyboardEventArgs { Key = pressedKey }])!);

        Assert.Equal(expectedTypeName, select.Instance.GetState(x => x.Value)?.TypeName);
    }

    private sealed class VariableTypeServiceStub : IVariableTypeService
    {
        public Task<IEnumerable<VariableTypeDescriptor>> GetVariableTypesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<VariableTypeDescriptor>>(
            [
                new("System.Boolean", "Boolean", "Primitives", null),
                new("System.Int32", "Integer", "Primitives", null),
                new("System.String", "String", "Text", null),
                new("System.Security.SecureString", "Secure String", "Security", null)
            ]);
    }

    private sealed class StorageDriverServiceStub : IStorageDriverService
    {
        public Task<IEnumerable<StorageDriverDescriptor>> GetStorageDriversAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<StorageDriverDescriptor>>([new("Workflow", "Workflow", 0, false)]);
    }

    private sealed class IdentityGeneratorStub : IIdentityGenerator
    {
        public string GenerateId() => "test-variable";
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string key] => new(key, key);
        public LocalizedString this[string key, params object[] arguments] => new(key, string.Format(key, arguments));
    }
}
