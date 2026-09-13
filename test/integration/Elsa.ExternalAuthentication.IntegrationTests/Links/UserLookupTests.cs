using System.Net.Http.Json;
using System.Text.Json;

namespace Elsa.ExternalAuthentication.IntegrationTests.Links;

public partial class ExternalIdentityLinkTests
{
    [Test]
    public async Task LookupReturnsOnlyMinimalTenantScopedDataAndSupportsCursorPaging()
    {
        var first = await Client.GetFromJsonAsync<UserList>("/external-authentication/user-options?pageSize=1");
        await Assert.That(first).IsNotNull();
        await Assert.That(first!.Items).HasSingleItem();
        await Assert.That(first.NextCursor).IsNotNull();
        await Assert.That(JsonSerializer.Serialize(first)).DoesNotContain("roles").WithComparison(StringComparison.OrdinalIgnoreCase);
        await Assert.That(JsonSerializer.Serialize(first)).DoesNotContain("password").WithComparison(StringComparison.OrdinalIgnoreCase);

        var second = await Client.GetFromJsonAsync<UserList>($"/external-authentication/user-options?pageSize=1&cursor={Uri.EscapeDataString(first.NextCursor!)}");
        await Assert.That(second).IsNotNull();
        await Assert.That(second!.Items).HasSingleItem();
        await Assert.That(second.Items).DoesNotContain(x => x.Id == "user-b");
    }

    private sealed record UserList(IReadOnlyCollection<UserDocument> Items, string? NextCursor);
    private sealed record UserDocument(string Id, string DisplayName);
}
