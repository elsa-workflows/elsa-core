using System.Text.Json;
using Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Models;
using Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Requests;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Clients;

public class ExternalAuthenticationClientContractTests
{
    [Test]
    public async Task NewSaveRequestSerializesHostScope()
    {
        var request = new SaveExternalAuthenticationConnectionRequest
        {
            AdapterSettings = JsonSerializer.SerializeToElement(new { })
        };
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);

        await Assert.That(document.RootElement.GetProperty("scope").GetProperty("kind").GetString()).IsEqualTo("host");
    }

    [Test]
    public async Task ConnectionDeserializesNamedShadowRelationships()
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

        await Assert.That(connection).IsNotNull();
        var deserializedConnection = connection!;
        await Assert.That(deserializedConnection.ShadowedBy?.Id).IsEqualTo("database-keycloak");
        await Assert.That(deserializedConnection.ShadowedBy?.DisplayName).IsEqualTo("Keycloak");
        await Assert.That(deserializedConnection.ShadowedBy?.Source).IsEqualTo("database");
        await Assert.That(deserializedConnection.Shadows).IsEmpty();
    }
}
