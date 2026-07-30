using System.Text.Json;
using Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Models;
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

    [Fact]
    public void ConnectionDeserializesNamedShadowRelationships()
    {
        var connection = JsonSerializer.Deserialize<ExternalAuthenticationConnection>(
            """
            {
              "id": "deployment-keycloak",
              "shadowed": true,
              "shadowedBy": {
                "id": "database-keycloak",
                "displayName": "Keycloak",
                "source": "database"
              },
              "shadows": []
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(connection);
        Assert.Equal("database-keycloak", connection.ShadowedBy?.Id);
        Assert.Equal("Keycloak", connection.ShadowedBy?.DisplayName);
        Assert.Equal("database", connection.ShadowedBy?.Source);
        Assert.Empty(connection.Shadows);
    }
}
