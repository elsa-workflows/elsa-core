using System.Net;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Xunit;

namespace Elsa.Studio.Core.Tests.Errors;

public sealed class AuthorizationFailureExtensionsTests
{
    private const string ForbiddenMessage = AuthorizationFailureExtensions.ForbiddenMessage;
    private const string UnauthorizedMessage = AuthorizationFailureExtensions.UnauthorizedMessage;

    // Core's permission checks answer 403 with an empty body. A module whose endpoints explain a refusal reads that
    // explanation with its own mapper; the shared formatter never echoes a body it does not know the shape of.
    [Theory]
    [InlineData(null)]
    [InlineData("""{ "error": "forbidden", "message": "Requires secrets:write." }""")]
    public void AForbiddenResponse_BecomesThePermissionGuidance_WhateverItsBody(string? body)
    {
        var exception = ApiExceptions.Create(HttpStatusCode.Forbidden, body);

        Assert.Equal("Response status code does not indicate success: 403 (Forbidden).", exception.Message);
        Assert.Equal(ForbiddenMessage, exception.ToUserMessage());
        Assert.True(exception.IsAuthorizationFailure());
    }

    [Fact]
    public void AnUnauthorizedResponse_AsksTheUserToSignInAgain()
    {
        var exception = ApiExceptions.Create(HttpStatusCode.Unauthorized);

        Assert.Equal(UnauthorizedMessage, exception.ToUserMessage());
        Assert.True(exception.IsAuthorizationFailure());
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Conflict)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public void AResponseWithAnyOtherStatus_KeepsItsOwnMessage(HttpStatusCode statusCode)
    {
        var exception = ApiExceptions.Create(statusCode);

        Assert.Equal(exception.Message, exception.ToUserMessage());
        Assert.False(exception.IsAuthorizationFailure());
    }

    [Fact]
    public void AForbiddenStatusRaisedOutsideRefit_BecomesThePermissionGuidance()
    {
        // HttpResponseMessage.EnsureSuccessStatusCode, as used by clients that stream rather than go through Refit.
        var exception = new HttpRequestException("Response status code does not indicate success: 403 (Forbidden).", null, HttpStatusCode.Forbidden);

        Assert.Equal(ForbiddenMessage, exception.ToUserMessage());
    }

    [Fact]
    public void AWrappedForbiddenResponse_BecomesThePermissionGuidance()
    {
        var exception = new InvalidOperationException("Loading the designer failed.", ApiExceptions.Create(HttpStatusCode.Forbidden));

        Assert.Equal(ForbiddenMessage, exception.ToUserMessage());
    }

    [Fact]
    public void AnotherResponseWrappingAForbiddenOne_IsNotReinterpreted()
    {
        var exception = new HttpRequestException("The gateway failed.", ApiExceptions.Create(HttpStatusCode.Forbidden), HttpStatusCode.BadGateway);

        Assert.Equal("The gateway failed.", exception.ToUserMessage());
        Assert.False(exception.IsAuthorizationFailure());
    }

    public static TheoryData<Exception> FailuresWithoutAResponse => new()
    {
        new InvalidOperationException("Something broke."),
        new UnauthorizedAccessException("Sign in to continue."),
        new OperationCanceledException("The request was cancelled."),
        new HttpRequestException("No such host is known.")
    };

    [Theory]
    [MemberData(nameof(FailuresWithoutAResponse))]
    public void AFailureWithoutAResponse_KeepsItsOwnMessage(Exception exception)
    {
        Assert.Equal(exception.Message, exception.ToUserMessage());
        Assert.False(exception.IsAuthorizationFailure());
    }

    [Fact]
    public void TheGuidance_IsLocalized_ButOtherMessagesAreLeftAlone()
    {
        var localizer = new DefaultLocalizer(new StubTranslations(new() { [ForbiddenMessage] = "Je hebt hier geen toegang toe." }));
        var serverError = ApiExceptions.Create(HttpStatusCode.InternalServerError);

        Assert.Equal("Je hebt hier geen toegang toe.", ApiExceptions.Create(HttpStatusCode.Forbidden).ToUserMessage(localizer));
        Assert.Equal(serverError.Message, serverError.ToUserMessage(localizer));
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, ForbiddenMessage)]
    [InlineData(HttpStatusCode.Unauthorized, UnauthorizedMessage)]
    [InlineData(HttpStatusCode.NotFound, null)]
    public void AStatusCode_HasGuidanceOnlyWhenItIsAnAuthorizationFailure(HttpStatusCode statusCode, string? expected)
    {
        Assert.Equal(expected, statusCode.GetAuthorizationFailureMessage());
    }

    [Fact]
    public void AnException_HasGuidanceOnlyWhenItReportsAnAuthorizationFailure()
    {
        Assert.Equal(ForbiddenMessage, ApiExceptions.Create(HttpStatusCode.Forbidden).GetAuthorizationFailureMessage());
        Assert.Null(ApiExceptions.Create(HttpStatusCode.InternalServerError).GetAuthorizationFailureMessage());
        Assert.Null(new InvalidOperationException("boom").GetAuthorizationFailureMessage());
    }

    [Fact]
    public void ABodylessResponse_PrefersTheGuidance_ThenTheReasonPhrase_ThenTheFallback()
    {
        Assert.Equal(ForbiddenMessage, HttpStatusCode.Forbidden.GetEmptyBodyFailureText("Forbidden", "fallback"));
        Assert.Equal("Not Found", HttpStatusCode.NotFound.GetEmptyBodyFailureText("Not Found", "fallback"));
        Assert.Equal("fallback", HttpStatusCode.NotFound.GetEmptyBodyFailureText(null, "fallback"));
    }
}
