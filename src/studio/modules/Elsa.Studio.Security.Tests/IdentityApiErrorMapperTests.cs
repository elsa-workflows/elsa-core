using System.Net;
using Elsa.Studio.Security.Services;
using Refit;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class IdentityApiErrorMapperTests
{
    [Fact]
    public void Describe_PreservesStructuredConflictCodeWithoutExposingRawDetails()
    {
        var exception = CreateApiException(HttpStatusCode.Conflict,
            """{ "error": "role_dependency_changed", "message": "Dependencies changed.", "details": { "secret": "hidden" } }""");

        var error = IdentityApiErrorMapper.Describe(exception);

        Assert.Equal("role_dependency_changed", error.Code);
        Assert.Equal("Dependencies changed.", error.Message);
        Assert.True(error.IsConflict);
        Assert.DoesNotContain("hidden", error.Message);
    }

    [Fact]
    public void Describe_FlattensValidationMessagesDeterministically()
    {
        var exception = CreateApiException(HttpStatusCode.BadRequest,
            """{ "statusCode": 400, "message": "Validation failed.", "errors": { "Permissions": ["Grant is invalid."], "Name": ["Name is required."] } }""");

        var error = IdentityApiErrorMapper.Describe(exception);

        Assert.Equal("validation_failed", error.Code);
        Assert.Equal("Name is required. Grant is invalid.", error.Message);
        Assert.True(error.IsValidation);
    }

    [Fact]
    public void Describe_DiscardsNonJsonBodies()
    {
        var exception = CreateApiException(HttpStatusCode.BadGateway, "<html>proxy details</html>");

        var error = IdentityApiErrorMapper.Describe(exception);

        Assert.Equal("unavailable", error.Code);
        Assert.DoesNotContain("proxy", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Describe_UsesTheSubjectInFallbackMessagesAndDefaultsToRoles()
    {
        var notFound = CreateApiException(HttpStatusCode.NotFound, string.Empty);
        var forbidden = CreateApiException(HttpStatusCode.Forbidden, string.Empty);

        Assert.Equal("The role no longer exists.", IdentityApiErrorMapper.Describe(notFound).Message);
        Assert.Equal("The user no longer exists.", IdentityApiErrorMapper.Describe(notFound, IdentityApiErrorMapper.UserSubject).Message);
        Assert.True(IdentityApiErrorMapper.Describe(forbidden, IdentityApiErrorMapper.UserSubject).IsAuthorization);
        Assert.StartsWith("User administration is unavailable", IdentityApiErrorMapper.Describe(new InvalidOperationException(), IdentityApiErrorMapper.UserSubject).Message);
    }

    private static ApiException CreateApiException(HttpStatusCode statusCode, string content) =>
        ApiException.Create(
            new HttpRequestMessage(HttpMethod.Post, "https://elsa.example/identity/roles"),
            HttpMethod.Post,
            new HttpResponseMessage(statusCode) { Content = new StringContent(content) },
            new RefitSettings()).GetAwaiter().GetResult();
}
