using System.Net;
using System.Text.Json;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Connections;

public sealed class ConnectionContractTests
{
    [Fact]
    public void ConnectionDetailDeserializesCanonicalLogoutModeAndStringWarnings()
    {
        var connection = JsonSerializer.Deserialize<ConnectionDetail>(
            """
            {
              "id": "connection-1",
              "upstreamLogoutMode": "user-choice",
              "validationWarnings": ["The provider metadata omitted an optional capability."]
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(connection);
        Assert.Equal("user-choice", connection.UpstreamLogoutMode);
        Assert.Equal("The provider metadata omitted an optional capability.", Assert.Single(connection.ValidationWarnings));
    }

    [Fact]
    public void ConnectionSummaryDeserializesNamedShadowRelationships()
    {
        var connection = JsonSerializer.Deserialize<ConnectionSummary>(
            """
            {
              "id": "deployment-keycloak",
              "shadowed": true,
              "shadowedBy": {
                "id": "database-keycloak",
                "displayName": "Keycloak",
                "source": "database"
              },
              "shadows": [
                {
                  "id": "legacy-keycloak",
                  "displayName": "Legacy Keycloak",
                  "source": "configuration"
                }
              ]
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(connection);
        Assert.Equal("database-keycloak", connection.ShadowedBy?.Id);
        Assert.Equal("Keycloak", connection.ShadowedBy?.DisplayName);
        Assert.Equal("legacy-keycloak", Assert.Single(connection.Shadows).Id);
    }

    [Fact]
    public void ValidationResultDeserializesStringWarnings()
    {
        var result = JsonSerializer.Deserialize<ConnectionValidationResult>(
            """
            {
              "valid": true,
              "errors": [],
              "warnings": ["The provider metadata omitted an optional capability."]
            }
            """,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.NotNull(result);
        Assert.Equal("The provider metadata omitted an optional capability.", Assert.Single(result.Warnings));
    }

    [Fact]
    public void StructuredManagementErrorExposesValidationDetails()
    {
        var error = ConnectionManagementError.Parse(
            HttpStatusCode.BadRequest,
            """
            {
              "error": "validation_failed",
              "message": "The connection is not valid for this operation.",
              "details": {
                "errors": [
                  {
                    "field": "adapterSettings.discoveryUrl",
                    "code": "invalid",
                    "message": "Discovery URL must be an absolute URI."
                  }
                ],
                "warnings": ["Provider metadata could not be verified."]
              }
            }
            """,
            "Response status code does not indicate success: 400 (Bad Request).");

        Assert.Equal("validation_failed", error.Code);
        Assert.Equal("The connection is not valid for this operation.", error.Message);
        var validationError = Assert.Single(error.Errors);
        Assert.Equal("adapterSettings.discoveryUrl", validationError.Field);
        Assert.Equal("invalid", validationError.Code);
        Assert.Equal("Discovery URL must be an absolute URI.", validationError.Message);
        Assert.Equal("Provider metadata could not be verified.", Assert.Single(error.Warnings));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("<html>not json</html>")]
    public void MalformedManagementErrorUsesTheHttpFallback(string? content)
    {
        var error = ConnectionManagementError.Parse(
            HttpStatusCode.BadRequest,
            content,
            "Response status code does not indicate success: 400 (Bad Request).");

        Assert.Equal("Response status code does not indicate success: 400 (Bad Request).", error.Message);
        Assert.Empty(error.Errors);
        Assert.Empty(error.Warnings);
    }

    [Fact]
    public void StructuredManagementErrorSkipsMalformedValidationEntries()
    {
        var error = ConnectionManagementError.Parse(
            HttpStatusCode.BadRequest,
            """
            {
              "error": "validation_failed",
              "message": "The connection is not valid for this operation.",
              "details": {
                "errors": [
                  { "field": "key", "code": "required", "message": "Key is required." },
                  "malformed",
                  { "field": "displayName", "code": "required", "message": "Display name is required." }
                ]
              }
            }
            """,
            "Response status code does not indicate success: 400 (Bad Request).");

        Assert.Equal("The connection is not valid for this operation.", error.Message);
        Assert.Equal(["key", "displayName"], error.Errors.Select(item => item.Field));
    }
}
