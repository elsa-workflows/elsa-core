using System.Net;
using Elsa.Activities.UnitTests.Http.Helpers;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Testing.Shared;
using Elsa.Workflows;

namespace Elsa.Activities.UnitTests.Http;

public class FlowSendHttpRequestTests
{
    [Theory]
    [InlineData("GET", "https://api.example.com/data", "{\"result\": \"success\"}", 200)]
    [InlineData("POST", "https://api.example.com/create", "{\"id\": 123}", 201)]
    [InlineData("PUT", "https://api.example.com/update", "{\"updated\": true}", 200)]
    public async Task Should_Send_Request_And_Set_Status_Code_Output(string method, string url, string jsonResponse, int expectedStatusCode)
    {
        // Arrange
        var expectedUrl = new Uri(url);
        var expectedMethod = new HttpMethod(method);
        var expectedHttpStatusCode = (HttpStatusCode)expectedStatusCode;
        var requestCapture = new SendHttpRequestTestHelpers.RequestCapture();
        var responseHandler = SendHttpRequestTestHelpers.CreateResponseHandler(expectedHttpStatusCode, jsonResponse, requestCapture);
        var flowSendHttpRequest = CreateFlowSendHttpRequest(expectedUrl, method);

        // Act
        var context = await ExecuteActivityAsync(flowSendHttpRequest, responseHandler);

        // Assert
        Assert.NotNull(requestCapture.CapturedRequest);
        Assert.Equal(expectedMethod, requestCapture.CapturedRequest.Method);
        Assert.Equal(expectedUrl, requestCapture.CapturedRequest.RequestUri);

        var statusCodeOutput = context.GetActivityOutput(() => flowSendHttpRequest.StatusCode);
        Assert.Equal(expectedStatusCode, statusCodeOutput);
    }

    [Theory]
    [InlineData("Bearer token123")]
    [InlineData("Basic YWRtaW46cGFzcw==")]
    [InlineData("ApiKey abc123")]
    public async Task Should_Add_Authorization_Header(string authorizationHeader)
    {
        // Arrange
        var expectedUrl = new Uri("https://api.example.com/secure");

        var requestCapture = new SendHttpRequestTestHelpers.RequestCapture();
        var responseHandler = SendHttpRequestTestHelpers.CreateResponseHandler(HttpStatusCode.OK, null, requestCapture);
        var flowSendHttpRequest = CreateFlowSendHttpRequest(expectedUrl, authorization: authorizationHeader);

        // Act
        await ExecuteActivityAsync(flowSendHttpRequest, responseHandler);

        // Assert
        Assert.NotNull(requestCapture.CapturedRequest);
        Assert.NotNull(requestCapture.CapturedRequest.Headers.Authorization);
        Assert.Equal(authorizationHeader, requestCapture.CapturedRequest.Headers.Authorization.ToString());
    }

    [Theory]
    [MemberData(nameof(StatusCodeOutcomeTestCases))]
    public async Task Should_Return_Outcome_Based_On_Status_Code(
        int[] expectedStatusCodes,
        HttpStatusCode actualStatusCode,
        string[] expectedOutcomes)
    {
        // Arrange
        var flowSendHttpRequest = CreateFlowSendHttpRequest(
            new("https://api.example.com/test"),
            expectedStatusCodes: expectedStatusCodes);

        var responseHandler = SendHttpRequestTestHelpers.CreateResponseHandler(actualStatusCode);

        // Act
        var context = await ExecuteActivityAsync(flowSendHttpRequest, responseHandler);

        // Assert - Activity should return outcomes based on status code match.
        var outcomes = context.GetOutcomes().ToList();
        Assert.Equal(expectedOutcomes.Length, outcomes.Count);
        foreach (var expectedOutcome in expectedOutcomes)
            Assert.Contains(expectedOutcome, outcomes);
    }

    public static IEnumerable<object[]> StatusCodeOutcomeTestCases()
    {
        // Expected status codes: 200, 404
        // Status code matches - returns status code + Done
        yield return [new[] { 200, 404 }, HttpStatusCode.OK, new[] { "200", "Done" }];
        yield return [new[] { 200, 404 }, HttpStatusCode.NotFound, new[] { "404", "Done" }];

        // Status code doesn't match - returns "Unmatched status code" + Done
        yield return [new[] { 200, 404 }, HttpStatusCode.InternalServerError, new[] { "Unmatched status code", "Done" }];

        // No expected status codes - returns only Done
        yield return [Array.Empty<int>(), HttpStatusCode.OK, new[] { "Done" }];
    }

    [Fact]
    public async Task Should_Return_FailedToConnect_Outcome_On_HttpRequestException()
    {
        // Arrange
        var flowSendHttpRequest = CreateFlowSendHttpRequest(new("https://api.example.com/error"));
        var responseHandler = SendHttpRequestTestHelpers.CreateExceptionHandler<HttpRequestException>("Connection failed");

        // Act
        var context = await ExecuteActivityAsync(flowSendHttpRequest, responseHandler);

        // Assert
        Assert.True(context.HasOutcome("Failed to connect"));
    }

    [Fact]
    public async Task Should_Return_Timeout_Outcome_On_TaskCanceledException()
    {
        // Arrange
        var flowSendHttpRequest = CreateFlowSendHttpRequest(new("https://api.example.com/timeout"));
        var responseHandler = SendHttpRequestTestHelpers.CreateExceptionHandler<TaskCanceledException>("Request timed out");

        // Act
        var context = await ExecuteActivityAsync(flowSendHttpRequest, responseHandler);

        // Assert
        Assert.True(context.HasOutcome("Timeout"));
    }

    [Fact]
    public async Task Should_Set_Response_Headers_Output()
    {
        // Arrange
        var expectedHeaders = new Dictionary<string, string>
        {
            { "Custom-Header", "CustomValue" },
            { "X-Rate-Limit", "100" }
        };

        var responseHandler = SendHttpRequestTestHelpers.CreateResponseHandler(HttpStatusCode.OK, additionalHeaders: expectedHeaders);
        var flowSendHttpRequest = CreateFlowSendHttpRequest(new("https://api.example.com/headers"));

        // Act
        var context = await ExecuteActivityAsync(flowSendHttpRequest, responseHandler);

        // Assert
        var responseHeadersObj = context.GetActivityOutput(() => flowSendHttpRequest.ResponseHeaders);
        var responseHeaders = responseHeadersObj as HttpHeaders;
        Assert.NotNull(responseHeaders);
        Assert.True(responseHeaders.ContainsKey("Custom-Header"));
        Assert.True(responseHeaders.ContainsKey("X-Rate-Limit"));
    }

    [Fact]
    public void Should_Have_Correct_Activity_Attributes()
    {
        var fixture = new ActivityTestFixture(new FlowSendHttpRequest());
        fixture.AssertActivityAttributes(
            expectedNamespace: "Elsa",
            expectedCategory: "HTTP",
            expectedDisplayName: "HTTP Request (flow)",
            expectedDescription: "Send an HTTP request.",
            expectedKind: ActivityKind.Task
        );
    }

    [Fact]
    public void Should_Have_Default_Expected_Status_Codes()
    {
        // Arrange
        var flowSendHttpRequest = new FlowSendHttpRequest();

        // Act - Get the default value
        var defaultValue = ((IActivityPropertyDefaultValueProvider)flowSendHttpRequest)
            .GetDefaultValue(typeof(FlowSendHttpRequest).GetProperty(nameof(FlowSendHttpRequest.ExpectedStatusCodes))!);

        // Assert
        var defaultStatusCodes = defaultValue as List<int>;
        Assert.NotNull(defaultStatusCodes);
        Assert.Single(defaultStatusCodes);
        Assert.Equal(200, defaultStatusCodes.First());
    }

    // Private helper methods
    private static FlowSendHttpRequest CreateFlowSendHttpRequest(
        Uri url,
        string method = "GET",
        object? content = null,
        string? contentType = null,
        string? authorization = null,
        int[]? expectedStatusCodes = null)
    {
        return new()
        {
            Url = new(url),
            Method = new(method),
            Content = content != null ? new Input<object?>(content) : null!,
            ContentType = contentType != null ? new Input<string?>(contentType) : null!,
            Authorization = authorization != null ? new Input<string?>(authorization) : null!,
            ExpectedStatusCodes = expectedStatusCodes != null ? new Input<ICollection<int>>(expectedStatusCodes) : null!
        };
    }

    private static Task<ActivityExecutionContext> ExecuteActivityAsync(
        FlowSendHttpRequest flowSendHttpRequest,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseHandler)
    {
        return new ActivityTestFixture(flowSendHttpRequest).WithHttpServices(responseHandler).ExecuteAsync();
    }
}
