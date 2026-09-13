using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Services;
using System.Threading.Tasks;

namespace Elsa.ExternalAuthentication.UnitTests.Foundational;

public class BrokerErrorFactoryTests
{
    [Test]
    [Arguments(BrokerErrorCategory.InvalidRequest, "invalid_request")]
    [Arguments(BrokerErrorCategory.MethodUnavailable, "method_unavailable")]
    [Arguments(BrokerErrorCategory.AuthenticationFailed, "authentication_failed")]
    [Arguments(BrokerErrorCategory.IdentityUnlinked, "identity_unlinked")]
    [Arguments(BrokerErrorCategory.FlowExpired, "flow_expired")]
    [Arguments(BrokerErrorCategory.FlowChanged, "flow_changed")]
    [Arguments(BrokerErrorCategory.AccessDenied, "access_denied")]
    [Arguments(BrokerErrorCategory.RateLimited, "rate_limited")]
    [Arguments(BrokerErrorCategory.TemporarilyUnavailable, "temporarily_unavailable")]
    [Arguments(BrokerErrorCategory.ServerError, "server_error")]
    public async Task CreatesDocumentedSafeErrorCategories(BrokerErrorCategory category, string error)
    {
        var result = BrokerErrorFactory.Create(category, "01JZSAFE-CORRELATION");

        await Assert.That(result.Error).IsEqualTo(error);
        await Assert.That(result.CorrelationId).IsEqualTo("01JZSAFE-CORRELATION");
        await Assert.That(result.Message).IsNotEmpty();
    }

    [Test]
    public async Task ReplacesUnsafeCorrelationIdsWithGeneratedTraceIds()
    {
        var result = BrokerErrorFactory.Create(BrokerErrorCategory.ServerError, "request\r\nleak");

        await Assert.That(result.CorrelationId).IsNotEqualTo("request\r\nleak");
        await Assert.That(result.CorrelationId.Length).IsEqualTo(32);
        foreach (var character in result.CorrelationId)
            await Assert.That(char.IsAsciiHexDigit(character)).IsTrue();
    }
}
