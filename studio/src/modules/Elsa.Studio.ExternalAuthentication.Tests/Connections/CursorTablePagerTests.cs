using Bunit;
using Elsa.Studio.ExternalAuthentication.Components.Pagination;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Connections;

public sealed class CursorTablePagerTests : BunitContext, IAsyncLifetime
{
    public CursorTablePagerTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        JSInterop.Setup<int>("mudpopoverHelper.countProviders").SetResult(1);
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void PageSizeSelect_IsGroupedInMudBlazorsNonGrowingPaginationDisplay()
    {
        var cut = Render<CursorTablePager>();

        var display = cut.Find(".mud-table-pagination-display");
        var pageSizeSelect = display.QuerySelector(".mud-table-pagination-select");

        Assert.NotNull(pageSizeSelect);
    }
}
