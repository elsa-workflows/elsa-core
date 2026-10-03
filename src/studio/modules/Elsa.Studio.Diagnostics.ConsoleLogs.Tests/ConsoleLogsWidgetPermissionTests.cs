using Bunit;
using Elsa.Studio.Authorization;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.ConsoleLogs.UI.Widgets;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Xunit;

namespace Elsa.Studio.Diagnostics.ConsoleLogs.Tests;

/// <summary>
/// The workflow instance viewer's console tabs are only offered to users who can view console logs.
/// </summary>
public sealed class ConsoleLogsWidgetPermissionTests : BunitContext, IAsyncLifetime
{
    public static TheoryData<IWidget> Widgets => new(
        new WorkflowInstanceConsoleLogsTabWidget(new KeyLocalizer()),
        new WorkflowInstanceConsoleLogsLeftPanelTabWidget(new KeyLocalizer()));

    [Theory]
    [MemberData(nameof(Widgets))]
    public void ConsoleTab_IsNotRenderedWithoutConsoleLogsViewPermission(IWidget widget)
    {
        var cut = Render<CascadingValue<UserPermissions>>(parameters => parameters
            .Add(x => x.Value, StubPermissionService.Grants("workflows/instances:view"))
            .Add(x => x.ChildContent, widget.Render(new Dictionary<string, object?> { ["WorkflowInstanceId"] = "instance-1" })));

        Assert.Empty(cut.FindComponents<MudTabPanel>());
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    private sealed class KeyLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => this[key];
    }
}
