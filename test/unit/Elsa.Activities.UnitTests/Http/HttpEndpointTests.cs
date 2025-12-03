using System.Text.Json;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Activities.UnitTests.Http;

public class HttpEndpointTests
{
    [Theory]
    [InlineData(true, "TestPolicy")]
    [InlineData(false, null)]
    public void Should_Configure_Authorization_Properties(bool authorize, string? policy)
    {
        // Arrange & Act
        var endpoint = CreateHttpEndpoint("/api/secure", new[] { "GET" }, authorize: authorize, policy: policy);

        // Assert
        Assert.Equal(authorize, endpoint.Authorize.Expression!.Value);
        if (policy != null)
        {
            Assert.Equal(policy, endpoint.Policy.Expression!.Value);
        }
    }

    [Fact]
    public void Should_Configure_MIME_Type_Whitelist()
    {
        // Arrange
        var allowedMimeTypes = new[] { "text/plain", "application/pdf" };
        
        // Act
        var endpoint = CreateHttpEndpoint("/api/upload", new[] { "POST" });
        endpoint.AllowedMimeTypes = new Input<ICollection<string>>(allowedMimeTypes);

        // Assert
        Assert.NotNull(endpoint.AllowedMimeTypes);
        var configuredMimeTypes = endpoint.AllowedMimeTypes.Expression!.Value as string[];
        Assert.NotNull(configuredMimeTypes);
        Assert.Equal(2, configuredMimeTypes.Length);
        Assert.Equal("text/plain", configuredMimeTypes[0]);
        Assert.Equal("application/pdf", configuredMimeTypes[1]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Should_Configure_Outcome_Exposure_Settings(bool exposeOutcomes)
    {
        // Arrange & Act
        var endpoint = CreateHttpEndpoint("/api/test", new[] { "POST" });
        endpoint.ExposeRequestTooLargeOutcome = exposeOutcomes;
        endpoint.ExposeFileTooLargeOutcome = exposeOutcomes;
        endpoint.ExposeInvalidFileExtensionOutcome = exposeOutcomes;
        endpoint.ExposeInvalidFileMimeTypeOutcome = exposeOutcomes;

        // Assert
        Assert.Equal(exposeOutcomes, endpoint.ExposeRequestTooLargeOutcome);
        Assert.Equal(exposeOutcomes, endpoint.ExposeFileTooLargeOutcome);
        Assert.Equal(exposeOutcomes, endpoint.ExposeInvalidFileExtensionOutcome);
        Assert.Equal(exposeOutcomes, endpoint.ExposeInvalidFileMimeTypeOutcome);
    }

    // TODO: Once `HttpEndpoint` is updated to produce a fault, update this test accordingly.
    [Fact]
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
        Assert.False(context.IsCompleted);
        Assert.True(context.WorkflowExecutionContext.Bookmarks.Any());
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

