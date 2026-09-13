using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Http;

public class HttpEndpointTests
{
    // TODO: Once `HttpEndpoint` is updated to produce a fault, update this test accordingly.
    [Test]
    public async Task Should_Create_Bookmark_When_No_Http_Context()
    {
        // Arrange
        var endpoint = CreateHttpEndpoint("/api/test", new[] { "GET" });
        var fixture = new ActivityTestFixture(endpoint)
            .ConfigureServices(services =>
            {
                // Don't provide HttpContextAccessor, simulating non-HTTP context
                var mockAccessor = Substitute.For<IHttpContextAccessor>();
                mockAccessor.HttpContext.Returns((HttpContext?)null);
                services.AddSingleton(mockAccessor);
            });

        // Act
        var context = await fixture.ExecuteAsync();

        // Assert
        // The activity should be suspended (not completed) with a bookmark
        await Assert.That(context.IsCompleted).IsFalse();
        await Assert.That(context.WorkflowExecutionContext.Bookmarks.Any()).IsTrue();
    }


    private static HttpEndpoint CreateHttpEndpoint(
        string path, 
        string[] methods, 
        bool authorize = false, 
        string? policy = null,
        TimeSpan? requestTimeout = null,
        long? requestSizeLimit = null)
    {
        var endpoint = new HttpEndpoint(new Input<string>(path))
        {
            SupportedMethods = new Input<ICollection<string>>(methods),
            Authorize = new Input<bool>(authorize)
        };

        if (policy != null)
            endpoint.Policy = new Input<string?>(policy);
        
        if (requestTimeout.HasValue)
            endpoint.RequestTimeout = new Input<TimeSpan?>(requestTimeout);
        
        if (requestSizeLimit.HasValue)
            endpoint.RequestSizeLimit = new Input<long?>(requestSizeLimit);

        return endpoint;
    }
}