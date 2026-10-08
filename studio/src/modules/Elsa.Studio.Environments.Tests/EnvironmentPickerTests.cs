using Bunit;
using Elsa.Studio.Environments.Components;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Services;
using Elsa.Studio.Testing;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Environments.Tests;

public sealed class EnvironmentPickerTests : BunitContext, IAsyncLifetime
{
    private readonly DefaultEnvironmentService _environments = new();

    public EnvironmentPickerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IEnvironmentService>(_environments);
        // Few enough environments that MudBlazor's flip logic would not move a menu that opens over its button.
        _environments.SetEnvironments([new() { Name = "Staging" }, new() { Name = "Production" }], "Staging");
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void EnvironmentPicker_OpensBelowItsButton() => MenuPopoverAssert.OpensBelowItsButton<EnvironmentPicker>(this);
}
