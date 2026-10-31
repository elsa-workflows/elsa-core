using Elsa.Studio.Extensions;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Elsa.Studio.Testing;
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

    [Theory]
    [InlineData("")]
    [InlineData("<html>Forbidden</html>")]
    [InlineData("""{"error":"forbidden"}""")]
    public void ManagementError_GivesAnUnexplainedForbiddenResponseThePermissionGuidance(string content)
    {
        var error = ConnectionManagementError.Parse(HttpStatusCode.Forbidden, content, "Response status code does not indicate success: 403 (Forbidden).");

        Assert.Equal(AuthorizationFailureExtensions.ForbiddenMessage, error.DisplayMessage);
    }

    [Theory]
    [InlineData("", AuthorizationFailureExtensions.ForbiddenMessage)]
    [InlineData("""{"error":"forbidden","message":"Revoking active sessions requires the external-authentication sessions-revoke permission."}""",
        "Revoking active sessions requires the external-authentication sessions-revoke permission.")]
    public void Describe_PresentsAForbiddenResponseByTheServersExplanation_OrTheGuidanceWithoutOne(string content, string expected)
    {
        Assert.Equal(expected, ConnectionManagementError.Describe(ApiExceptions.Create(HttpStatusCode.Forbidden, content)));
    }

    [Fact]
    public void Describe_LeavesEveryOtherFailureAsItWas()
    {
        var conflict = ApiExceptions.Create(HttpStatusCode.Conflict, """{"error":"conflict","message":"The connection changed."}""");
        var unreachable = new HttpRequestException("No such host is known.");

        Assert.Equal(conflict.Message, ConnectionManagementError.Describe(conflict));
        Assert.Equal(unreachable.Message, ConnectionManagementError.Describe(unreachable));
    }

    [Fact]
    public void PresentError_ExplainsARefusedOperationInsteadOfTheCallersFallback()
    {
        var forbidden = ApiExceptions.Create(HttpStatusCode.Forbidden);
        var unreachable = new HttpRequestException("No such host is known.");

        Assert.Equal(AuthorizationFailureExtensions.ForbiddenMessage, ConnectionOperationActions.PresentError(forbidden, ConnectionOperationActions.TestFailedMessage));
        Assert.Equal(ConnectionOperationActions.TestFailedMessage, ConnectionOperationActions.PresentError(unreachable, ConnectionOperationActions.TestFailedMessage));
    }

    private static ConnectionSummary CreateDatabaseConnection(string id, bool archived) => new()
    {
        Id = id,
        Key = "keycloak-idp",
        Source = "database",
        Archived = archived
    };
}
