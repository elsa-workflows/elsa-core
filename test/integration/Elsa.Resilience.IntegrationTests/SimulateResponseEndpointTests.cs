using System.Net;
using Elsa.Resilience.Options;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using TUnit.AspNetCore;

namespace Elsa.Resilience.IntegrationTests;

public class SimulateResponseEndpointTests : WebApplicationTest<ResilienceWebApplicationFactory, ResilienceTestEntryPoint>
{
    private readonly TestTimeProvider _timeProvider = new();
    private HttpClient? _httpClient;

    private HttpClient HttpClient => _httpClient ??= Factory.CreateClient();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.AddSingleton<TimeProvider>(_timeProvider);
        services.AddScoped<IRetryAttemptReader>(_ => VoidRetryAttemptReader.Instance);
        services.AddScoped(_ =>
        {
            var catalog = Substitute.For<IResilienceStrategyCatalog>();
            catalog.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
            return catalog;
        });
    }

    [Test]
    public async Task Get_WhenAnonymousAndSecurityEnabled_DoesNotCreateSessionState()
    {
        var response = await HttpClient.GetAsync("/simulate-response?sessionId=anon&codes=[500,200]");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);

        var authenticatedResponse = await GetAuthenticatedAsync("/simulate-response?sessionId=anon&codes=[500,200]");
        await Assert.That(authenticatedResponse.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task Get_WhenCodesAreMalformed_ReturnsBadRequest()
    {
        var response = await GetAuthenticatedAsync("/simulate-response?codes=not-json");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Get_WhenSessionIdExceedsMaxLength_ReturnsBadRequest()
    {
        var response = await GetAuthenticatedAsync("/simulate-response?sessionId=exceeds-sixteen-chars");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Get_WhenSessionIdIsAtMaxLength_AcceptsRequest()
    {
        var response = await GetAuthenticatedAsync("/simulate-response?sessionId=1234567890123456&codes=[200]");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Get_WhenCodesQueryExceedsMaxLength_ReturnsBadRequest()
    {
        var response = await GetAuthenticatedAsync($"/simulate-response?codes={new string('1', 33)}");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Get_WhenCodesExceedMaxCount_ReturnsBadRequest()
    {
        var response = await GetAuthenticatedAsync("/simulate-response?codes=[500,503,200,201]");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [Arguments(99)]
    [Arguments(600)]
    public async Task Get_WhenCodesAreOutOfRange_ReturnsBadRequest(int statusCode)
    {
        var response = await GetAuthenticatedAsync($"/simulate-response?codes=[{statusCode}]");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Get_WhenSessionCapacityIsReached_ReturnsTooManyRequestsUntilStateExpires()
    {
        await Assert.That((await GetAuthenticatedAsync("/simulate-response?sessionId=first&codes=[500,200]")).StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
        await Assert.That((await GetAuthenticatedAsync("/simulate-response?sessionId=second&codes=[500,200]")).StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);

        var rejected = await GetAuthenticatedAsync("/simulate-response?sessionId=third&codes=[500,200]");
        await Assert.That(rejected.StatusCode).IsEqualTo(HttpStatusCode.TooManyRequests);

        _timeProvider.Advance(TimeSpan.FromSeconds(2));

        var acceptedAfterExpiration = await GetAuthenticatedAsync("/simulate-response?sessionId=third&codes=[500,200]");
        await Assert.That(acceptedAfterExpiration.StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
    }

    [Test]
    public async Task Get_WhenExistingSessionUsesShorterCodes_DoesNotReadBeyondCodes()
    {
        await Assert.That((await GetAuthenticatedAsync("/simulate-response?sessionId=reused&codes=[500,503,200]")).StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);

        var response = await GetAuthenticatedAsync("/simulate-response?sessionId=reused&codes=[200]");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Get_WhenDifferentIdentitiesUseSameSessionId_TracksSessionsIndependently()
    {
        await Assert.That((await GetAuthenticatedAsync("/simulate-response?sessionId=shared&codes=[500,200]", "alice")).StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);
        await Assert.That((await GetAuthenticatedAsync("/simulate-response?sessionId=shared&codes=[500,200]", "bob")).StatusCode).IsEqualTo(HttpStatusCode.InternalServerError);

        var response = await GetAuthenticatedAsync("/simulate-response?sessionId=shared&codes=[500,200]", "alice");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    private async Task<HttpResponseMessage> GetAuthenticatedAsync(string requestUri, string identity = "test-user")
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add(ResilienceTestAuthenticationHandler.PermissionHeader, "*");
        request.Headers.Add(ResilienceTestAuthenticationHandler.IdentityHeader, identity);
        return await HttpClient.SendAsync(request);
    }

    private sealed class TestTimeProvider : TimeProvider
    {
        private DateTimeOffset _now = DateTimeOffset.Parse("2026-05-20T00:00:00Z");

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan timeSpan)
        {
            _now = _now.Add(timeSpan);
        }
    }

}
