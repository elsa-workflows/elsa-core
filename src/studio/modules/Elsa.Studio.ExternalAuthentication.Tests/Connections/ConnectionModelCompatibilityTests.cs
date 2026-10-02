using System.Text.Json;
using Elsa.Studio.ExternalAuthentication.Models;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Connections;

public class ConnectionModelCompatibilityTests
{
    [Fact]
    public void Validation_warnings_use_the_management_contract_shape()
    {
        const string payload = """
            {
              "valid": true,
              "errors": [],
              "warnings": ["Review the configured issuer."]
            }
            """;

        var result = JsonSerializer.Deserialize<ConnectionValidationResult>(
            payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal("Review the configured issuer.", Assert.Single(result!.Warnings));
    }
}
