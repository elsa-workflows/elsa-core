using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using System.Net;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Connections;

public class ConnectionManagementUiStateTests
{
    [Fact]
    public void OverrideDiscovery_FindsArchivedRecordsAndPrefersAnActiveMatch()
    {
        var archived = CreateDatabaseConnection("archived", archived: true);
        var active = CreateDatabaseConnection("active", archived: false);

        Assert.Same(
            archived,
            ConnectionOverrideDiscovery.FindExisting([archived], "keycloak-idp"));
        Assert.Same(
            active,
            ConnectionOverrideDiscovery.FindExisting([archived, active], "keycloak-idp"));
    }

    [Fact]
    public void ManagementError_PresentsSafeCodeAndCorrelationIdentifier()
    {
        const string correlationId = "0af7651916cd43dd8448eb211c80319c";
        var error = ConnectionManagementError.Parse(
            HttpStatusCode.BadRequest,
            $$"""{"error":"validation_failed","message":"The connection is invalid.","correlationId":"{{correlationId}}"}""",
            "fallback");

        Assert.Equal("validation_failed", error.Code);
        Assert.Equal(correlationId, error.CorrelationId);
        Assert.Equal(
            $"The connection is invalid. Error: validation_failed. Correlation ID: {correlationId}.",
            error.OperationalDisplayMessage);
    }

    [Fact]
    public void ManagementError_DoesNotPresentUnsafeCorrelationIdentifier()
    {
        var error = ConnectionManagementError.Parse(
            HttpStatusCode.BadRequest,
            """{"error":"validation_failed","message":"The connection is invalid.","correlationId":"<script>alert(1)</script>"}""",
            "fallback");

        Assert.Null(error.CorrelationId);
        Assert.DoesNotContain("script", error.OperationalDisplayMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static ConnectionSummary CreateDatabaseConnection(string id, bool archived) => new()
    {
        Id = id,
        Key = "keycloak-idp",
        Source = "database",
        Archived = archived
    };
}
