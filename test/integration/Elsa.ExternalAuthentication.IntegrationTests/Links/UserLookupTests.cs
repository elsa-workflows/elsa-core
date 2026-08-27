using System.Net.Http.Json;
using System.Text.Json;

namespace Elsa.ExternalAuthentication.IntegrationTests.Links;

public partial class ExternalIdentityLinkTests
{
    [Fact]
    public async Task LookupReturnsOnlyMinimalTenantScopedDataAndSupportsCursorPaging()
    {
        var first = await Client.GetFromJsonAsync<UserList>("/external-authentication/user-options?pageSize=1");
        Assert.NotNull(first);
        Assert.Single(first!.Items);
        Assert.NotNull(first.NextCursor);
        Assert.DoesNotContain("roles", JsonSerializer.Serialize(first), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", JsonSerializer.Serialize(first), StringComparison.OrdinalIgnoreCase);

        var second = await Client.GetFromJsonAsync<UserList>($"/external-authentication/user-options?pageSize=1&cursor={Uri.EscapeDataString(first.NextCursor!)}");
        Assert.NotNull(second);
        Assert.Single(second!.Items);
        Assert.DoesNotContain(second.Items, x => x.Id == "user-b");
    }

    private sealed record UserList(IReadOnlyCollection<UserDocument> Items, string? NextCursor);
    private sealed record UserDocument(string Id, string DisplayName);
}
