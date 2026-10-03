using Elsa.Studio.Authorization;
using Elsa.Studio.Http.Webhooks;
using Elsa.Studio.Http.Webhooks.Menu;
using Elsa.Studio.Http.Webhooks.Pages;
using Elsa.Studio.Testing;
using Xunit;

namespace Elsa.Studio.Administration.Tests;

public sealed class WebhooksGatingTests
{
    [Fact]
    public async Task WebhooksMenu_RequiresTheCataloguedViewPermission()
    {
        var item = Assert.Single(await new WebhooksMenu(new TestLocalizer()).GetMenuItemsAsync());

        var required = Assert.Single(item.RequiredPermissions);
        Assert.Equal(WebhookPermissions.Webhooks, required.Resource);
        Assert.Equal(PermissionVerbs.View, required.Verb);
    }

    [Fact]
    public void WebhooksPage_DeclaresTheSameViewPermission()
    {
        var required = Assert.Single(RequirePermissionAttribute.GetRequiredPermissions(typeof(Index)));

        Assert.Equal(WebhookPermissions.Webhooks, required.Resource);
        Assert.Equal(PermissionVerbs.View, required.Verb);
    }
}
