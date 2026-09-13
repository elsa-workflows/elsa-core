using System.Net;
using Elsa.Http;
using Elsa.Http.ContentWriters;
using Elsa.Http.Options;
using Elsa.Http.Parsers;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Elsa.Workflows.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Threading.Tasks;

namespace Elsa.Activities.UnitTests.Http;

public class WriteHttpResponseTests
{
    [Test]
    [Arguments(HttpStatusCode.OK, 200)]
    [Arguments(HttpStatusCode.Created, 201)]
    [Arguments(HttpStatusCode.NotFound, 404)]
    [Arguments(HttpStatusCode.InternalServerError, 500)]
    public async Task Should_Set_Correct_Status_Code(HttpStatusCode statusCode, int expectedStatusCode)
    {
        // Arrange
        var (activity, httpContext) = CreateWriteHttpResponseActivity();
        activity.StatusCode = new Input<HttpStatusCode>(statusCode);

        // Act
        var context = await ExecuteActivityAsync(activity, httpContext);

        // Assert
        await Assert.That(httpContext.Response.StatusCode).IsEqualTo(expectedStatusCode);
        await Assert.That(context.IsCompleted).IsTrue();
    }

    [Test]
    [Arguments("Hello World", "text/plain", "Hello World")]
    [Arguments("{\"name\": \"John\"}", "application/json", "{\"name\": \"John\"}")]
    [Arguments("<root><name>John</name></root>", "application/xml", "<root><name>John</name></root>")]
    public async Task Should_Write_String_Content_With_Correct_Content_Type(string content, string contentType, string expectedContent)
    {
        // Arrange
        var (activity, httpContext) = CreateWriteHttpResponseActivity();
        activity.Content = new Input<object?>(content);
        activity.ContentType = new Input<string?>(contentType);

        // Act
        await ExecuteActivityAsync(activity, httpContext);

        // Assert
        await Assert.That(httpContext.Response.ContentType).IsEqualTo(contentType);
        var responseContent = GetResponseContent(httpContext);
        await Assert.That(responseContent).IsEqualTo(expectedContent);
    }

    [Test]
    public async Task Should_Serialize_Object_To_Json_When_No_Content_Type_Specified()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteHttpResponseActivity();
        var testObject = new { Name = "John", Age = 30 };
        activity.Content = new Input<object?>(testObject);

        // Act
        await ExecuteActivityAsync(activity, httpContext);

        // Assert
        await Assert.That(httpContext.Response.ContentType).IsEqualTo("application/json");
        var responseContent = GetResponseContent(httpContext);
        await Assert.That(responseContent).Contains("John");
        await Assert.That(responseContent).Contains("30");
    }

    [Test]
    [Arguments("Custom-Header", "CustomValue")]
    [Arguments("X-Rate-Limit", "100")]
    [Arguments("Cache-Control", "no-cache")]
    public async Task Should_Add_Response_Headers(string headerName, string headerValue)
    {
        // Arrange
        var (activity, httpContext) = CreateWriteHttpResponseActivity();
        var headers = new HttpHeaders { { headerName, new[] { headerValue } } };
        activity.ResponseHeaders = new Input<HttpHeaders?>(headers);

        // Act
        await ExecuteActivityAsync(activity, httpContext);

        // Assert
        await Assert.That(httpContext.Response.Headers.ContainsKey(headerName)).IsTrue();
        await Assert.That(httpContext.Response.Headers[headerName].ToString()).IsEqualTo(headerValue);
    }

    [Test]
    public async Task Should_Add_Multiple_Response_Headers()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteHttpResponseActivity();
        var headers = new HttpHeaders
        {
            { "Custom-Header-1", new[] { "Value1" } },
            { "Custom-Header-2", new[] { "Value2" } },
            { "X-Rate-Limit", new[] { "100" } }
        };
        activity.ResponseHeaders = new Input<HttpHeaders?>(headers);

        // Act
        await ExecuteActivityAsync(activity, httpContext);

        // Assert
        await Assert.That(httpContext.Response.Headers.ContainsKey("Custom-Header-1")).IsTrue();
        await Assert.That(httpContext.Response.Headers.ContainsKey("Custom-Header-2")).IsTrue();
        await Assert.That(httpContext.Response.Headers.ContainsKey("X-Rate-Limit")).IsTrue();
        await Assert.That(httpContext.Response.Headers["Custom-Header-1"].ToString()).IsEqualTo("Value1");
        await Assert.That(httpContext.Response.Headers["Custom-Header-2"].ToString()).IsEqualTo("Value2");
        await Assert.That(httpContext.Response.Headers["X-Rate-Limit"].ToString()).IsEqualTo("100");
    }

    [Test]
    public async Task Should_Not_Write_Content_When_Status_Code_Is_NoContent()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteHttpResponseActivity();
        activity.StatusCode = new Input<HttpStatusCode>(HttpStatusCode.NoContent);
        activity.Content = new Input<object?>("This should not be written");

        // Act
        await ExecuteActivityAsync(activity, httpContext);

        // Assert
        await Assert.That(httpContext.Response.StatusCode).IsEqualTo(204);
        var responseContent = GetResponseContent(httpContext);
        await Assert.That(responseContent).IsEmpty();
    }

    [Test]
    public async Task Should_Handle_Null_Content()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteHttpResponseActivity();
        activity.Content = new Input<object?>((object?)null);

        // Act
        await ExecuteActivityAsync(activity, httpContext);

        // Assert
        await Assert.That(httpContext.Response.StatusCode).IsEqualTo(200);
        var responseContent = GetResponseContent(httpContext);
        await Assert.That(responseContent).IsEmpty();
    }

    [Test]
    public async Task Should_Call_CompleteAsync_When_WriteHttpResponseSynchronously_Is_True()
    {
        // Arrange
        var activity = new WriteHttpResponse();
        var mockHttpContext = Substitute.For<HttpContext>();
        var mockResponse = Substitute.For<HttpResponse>();
        var mockHeaders = new HeaderDictionary();
        var responseBody = new MemoryStream();
        
        mockResponse.StatusCode.Returns(200);
        mockResponse.Headers.Returns(mockHeaders);
        mockResponse.Body.Returns(responseBody);
        mockHttpContext.Response.Returns(mockResponse);

        // Act
        await ExecuteActivityAsync(activity, mockHttpContext, writeResponseSynchronously: true);

        // Assert
        await mockResponse.Received(1).CompleteAsync();
    }

    [Test]
    public async Task Should_Not_Call_CompleteAsync_When_WriteHttpResponseSynchronously_Is_False()
    {
        // Arrange
        var activity = new WriteHttpResponse();
        var mockHttpContext = Substitute.For<HttpContext>();
        var mockResponse = Substitute.For<HttpResponse>();
        var mockHeaders = new HeaderDictionary();
        var responseBody = new MemoryStream();
        
        mockResponse.StatusCode.Returns(200);
        mockResponse.Headers.Returns(mockHeaders);
        mockResponse.Body.Returns(responseBody);
        mockHttpContext.Response.Returns(mockResponse);

        // Act
        await ExecuteActivityAsync(activity, mockHttpContext, writeResponseSynchronously: false);

        // Assert
        await mockResponse.DidNotReceive().CompleteAsync();
    }

    [Test]
    public async Task Should_Fault_When_No_HttpContext_Available()
    {
        // Arrange
        var activity = new WriteHttpResponse();
        var fixture = new ActivityTestFixture(activity);
        fixture.ConfigureServices(services =>
        {
            var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
            mockHttpContextAccessor.HttpContext.Returns((HttpContext?)null);
            services.AddSingleton(mockHttpContextAccessor);
            services.AddSingleton(CreateMockHttpActivityOptions());
            AddHttpContentFactories(services);
            services.AddSingleton(Substitute.For<IStimulusHasher>());
        });

        // Act + Assert
        await Assert.ThrowsExactlyAsync<FaultException>(() => fixture.ExecuteAsync());
    }

    [Test]
    public async Task Should_Use_Default_Status_Code_When_Not_Specified()
    {
        // Arrange
        var (activity, httpContext) = CreateWriteHttpResponseActivity();
        // StatusCode is not set, should default to OK (200)

        // Act
        await ExecuteActivityAsync(activity, httpContext);

        // Assert
        await Assert.That(httpContext.Response.StatusCode).IsEqualTo(200);
    }
    
    private static (WriteHttpResponse activity, HttpContext httpContext) CreateWriteHttpResponseActivity()
    {
        var activity = new WriteHttpResponse();
        var httpContext = new DefaultHttpContext
        {
            Response =
            {
                Body = new MemoryStream()
            }
        };

        return (activity, httpContext);
    }

    private static async Task<ActivityExecutionContext> ExecuteActivityAsync(WriteHttpResponse activity, HttpContext httpContext, bool writeResponseSynchronously = false)
    {
        var fixture = new ActivityTestFixture(activity);
        fixture.ConfigureServices(services =>
        {
            var mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
            mockHttpContextAccessor.HttpContext.Returns(httpContext);
            services.AddSingleton(mockHttpContextAccessor);
            services.AddSingleton(CreateMockHttpActivityOptions(writeResponseSynchronously));
            AddHttpContentFactories(services);
            AddHttpContentParsers(services);
            services.AddLogging();
        });

        return await fixture.ExecuteAsync();
    }

    private static IOptions<HttpActivityOptions> CreateMockHttpActivityOptions(bool writeResponseSynchronously = false)
    {
        var options = new HttpActivityOptions
        {
            WriteHttpResponseSynchronously = writeResponseSynchronously
        };
        var mockOptions = Substitute.For<IOptions<HttpActivityOptions>>();
        mockOptions.Value.Returns(options);
        return mockOptions;
    }

    private static void AddHttpContentFactories(IServiceCollection services)
    {
        services.AddSingleton<IHttpContentFactory, JsonContentFactory>();
        services.AddSingleton<IHttpContentFactory, TextContentFactory>();
        services.AddSingleton<IHttpContentFactory, XmlContentFactory>();
        services.AddSingleton<IHttpContentFactory, FormUrlEncodedHttpContentFactory>();
    }

    private static void AddHttpContentParsers(IServiceCollection services)
    {
        services.AddSingleton<IHttpContentParser, JsonHttpContentParser>();
        services.AddSingleton<IHttpContentParser, PlainTextHttpContentParser>();
        services.AddSingleton<IHttpContentParser, XmlHttpContentParser>();
        services.AddSingleton<IHttpContentParser, TextHtmlHttpContentParser>();
        services.AddSingleton<IHttpContentParser, FileHttpContentParser>();
    }

    private static string GetResponseContent(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(httpContext.Response.Body);
        return reader.ReadToEnd();
    }
}
