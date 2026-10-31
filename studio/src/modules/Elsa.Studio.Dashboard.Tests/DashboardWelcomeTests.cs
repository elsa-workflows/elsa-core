using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Components;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public sealed class DashboardWelcomeTests : BunitContext
{
    private readonly RecordingLogger _logger = new();

    public DashboardWelcomeTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<ILogger<DashboardWelcome>>(_logger);
        Services.AddSingleton<IMenuService>(new ThrowingMenuService());
    }

    [Fact]
    public void WhenTheNavigationMenuCannotBeResolved_TheWelcomeRendersWithoutShortcuts_AndTheFailureIsLogged()
    {
        var cut = Render<DashboardWelcome>();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("[data-testid='dashboard-welcome']")));
        Assert.Contains("No dashboard widgets are available to your role.", cut.Markup);
        Assert.Empty(cut.FindAll("a"));
        Assert.Empty(cut.FindAll("[data-testid='no-accessible-pages']"));
        var entry = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.IsType<InvalidOperationException>(entry.Exception);
    }

    private sealed class ThrowingMenuService : IMenuService
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("The menu is unavailable.");
        public ValueTask<IEnumerable<MenuItemGroup>> GetMenuItemGroupsAsync(CancellationToken cancellationToken = default) => throw new InvalidOperationException("The menu is unavailable.");
    }

    private sealed class RecordingLogger : ILogger<DashboardWelcome>
    {
        public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) => Entries.Add((logLevel, exception));
    }
}
