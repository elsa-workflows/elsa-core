using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class BrokerErrorFactoryTests
{
    [Theory]
    [InlineData(BrokerErrorCategory.InvalidRequest, "invalid_request")]
    [InlineData(BrokerErrorCategory.MethodUnavailable, "method_unavailable")]
    [InlineData(BrokerErrorCategory.AuthenticationFailed, "authentication_failed")]
    [InlineData(BrokerErrorCategory.IdentityUnlinked, "identity_unlinked")]
    [InlineData(BrokerErrorCategory.FlowExpired, "flow_expired")]
    [InlineData(BrokerErrorCategory.FlowChanged, "flow_changed")]
    [InlineData(BrokerErrorCategory.AccessDenied, "access_denied")]
    [InlineData(BrokerErrorCategory.RateLimited, "rate_limited")]
    [InlineData(BrokerErrorCategory.TemporarilyUnavailable, "temporarily_unavailable")]
    [InlineData(BrokerErrorCategory.ServerError, "server_error")]
    public void CreatesDocumentedSafeErrorCategories(BrokerErrorCategory category, string error)
    {
        var result = BrokerErrorFactory.Create(category, "01JZSAFE-CORRELATION");

        Assert.Equal(error, result.Error);
        Assert.Equal("01JZSAFE-CORRELATION", result.CorrelationId);
        Assert.NotEmpty(result.Message);
    }

    [Fact]
    public void ReplacesUnsafeCorrelationIdsWithGeneratedTraceIds()
    {
        var result = BrokerErrorFactory.Create(BrokerErrorCategory.ServerError, "request\r\nleak");

        Assert.NotEqual("request\r\nleak", result.CorrelationId);
        Assert.Equal(32, result.CorrelationId.Length);
        Assert.All(result.CorrelationId, character => Assert.True(char.IsAsciiHexDigit(character)));
    }
}
