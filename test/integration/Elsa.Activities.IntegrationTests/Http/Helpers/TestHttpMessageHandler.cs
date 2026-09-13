namespace Elsa.Activities.IntegrationTests.Http.Helpers;

/// <summary>
/// A project-local HTTP message handler for exercising HTTP activities without depending on another test project.
/// </summary>
public sealed class TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> sendAsync) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        sendAsync(request, cancellationToken);
}
