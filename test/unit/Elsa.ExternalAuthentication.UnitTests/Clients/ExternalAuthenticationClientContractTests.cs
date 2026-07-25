using System.Text.Json;
using Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Requests;

namespace Elsa.ExternalAuthentication.UnitTests.Clients;

public class ExternalAuthenticationClientContractTests
{
    [Fact]
    public void NewSaveRequestSerializesHostScope()
    {
        var request = new SaveExternalAuthenticationConnectionRequest
        {
            AdapterSettings = JsonSerializer.SerializeToElement(new { })
        };
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);

        Assert.Equal("host", document.RootElement.GetProperty("scope").GetProperty("kind").GetString());
    }
}
