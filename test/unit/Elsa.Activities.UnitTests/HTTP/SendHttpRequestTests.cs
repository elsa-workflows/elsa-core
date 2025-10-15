using System.Net;
using Elsa.Activities.UnitTests.Helpers;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Workflows;
using NSubstitute;

namespace Elsa.Activities.UnitTests.HTTP;

public class SendHttpRequestTests
{
    [Theory]
    [InlineData("GET", "https://api.example.com/data", "{\"result\": \"success\"}", 200)]
    [InlineData("POST", "https://api.example.com/create", "{\"id\": 123}", 201)]
    [InlineData("PUT", "https://api.example.com/update", "{\"updated\": true}", 200)]
    public async Task Should_Send_Request_And_Handle_Success_Response(string method, string url, string jsonResponse, int expectedStatusCode)
    {
        // Arrange
        var expectedUrl = new Uri(url);
        var expectedMethod = new HttpMethod(method);
        var expectedHttpStatusCode = (HttpStatusCode)expectedStatusCode;
        
        var requestCapture = new RequestCapture();
        var responseHandler = CreateResponseHandler(expectedHttpStatusCode, jsonResponse, requestCapture);
        var sendHttpRequest = CreateSendHttpRequest(expectedUrl, method);

        // Act
        var context = await ExecuteActivityWithResponseHandler(sendHttpRequest, responseHandler);

        // Assert
        Assert.NotNull(requestCapture.CapturedRequest);
        Assert.Equal(expectedMethod, requestCapture.CapturedRequest.Method);
        Assert.Equal(expectedUrl, requestCapture.CapturedRequest.RequestUri);
        
        var statusCodeOutput = context.GetExecutionOutput(_ => sendHttpRequest.StatusCode);
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
        
        var requestCapture = new RequestCapture();
        var responseHandler = CreateResponseHandler(HttpStatusCode.OK, null, requestCapture);
        var sendHttpRequest = CreateSendHttpRequest(expectedUrl, authorization: authorizationHeader);

        // Act
        await ExecuteActivityWithResponseHandler(sendHttpRequest, responseHandler);

        // Assert
        Assert.NotNull(requestCapture.CapturedRequest);
        Assert.NotNull(requestCapture.CapturedRequest.Headers.Authorization);
        Assert.Equal(authorizationHeader, requestCapture.CapturedRequest.Headers.Authorization.ToString());
    }

    [Theory]
    [InlineData(404, "MatchingStatusCode")]
    [InlineData(500, "UnmatchedStatusCode")]
    public async Task Should_Schedule_Correct_Activity_Based_On_Status_Code(int responseStatusCode, string expectedSchedulingBehavior)
    {
        // Arrange
        var mockActivity404 = CreateMockActivity();
        var mockActivity200 = CreateMockActivity();
        var mockUnmatchedActivity = CreateMockActivity();
        
        var sendHttpRequest = new SendHttpRequest
        {
            Url = new Input<Uri?>(new Uri("https://api.example.com/test")),
            Method = new Input<string>("GET"),
            ExpectedStatusCodes = new List<HttpStatusCodeCase>
            {
                new(200, mockActivity200),
                new(404, mockActivity404)
            },
            UnmatchedStatusCode = mockUnmatchedActivity
        };

        var childActivities = new[] { mockActivity404, mockActivity200, mockUnmatchedActivity };
        var responseHandler = CreateResponseHandler((HttpStatusCode)responseStatusCode);

        // Act - Execute with child activities in workflow context
        var context = await ExecuteActivityWithChildActivitiesInBackgroundMode(
            sendHttpRequest, childActivities, responseHandler);

        // Assert - Get scheduled activities and verify correct one was scheduled
        var scheduledActivities = context.GetBackgroundScheduledActivities().ToList();
        
        switch (expectedSchedulingBehavior)
        {
            case "MatchingStatusCode":
                Assert.Single(scheduledActivities);
                var scheduledActivity404 = scheduledActivities.First();
                var expectedNode404 = context.WorkflowExecutionContext.FindNodeByActivity(mockActivity404);
                Assert.Equal(expectedNode404?.NodeId, scheduledActivity404.ActivityNodeId);
                break;
            case "UnmatchedStatusCode":
                Assert.Single(scheduledActivities);
                var scheduledUnmatched = scheduledActivities.First();
                var expectedUnmatchedNode = context.WorkflowExecutionContext.FindNodeByActivity(mockUnmatchedActivity);
                Assert.Equal(expectedUnmatchedNode?.NodeId, scheduledUnmatched.ActivityNodeId);
                break;
        }
    }

    [Fact]
    public async Task Should_Schedule_Null_Activity_When_No_Status_Code_Cases_Match_And_No_Unmatched_Handler()
    {
        // Arrange
        var mockActivity200 = CreateMockActivity();
        
        var sendHttpRequest = new SendHttpRequest
        {
            Url = new Input<Uri?>(new Uri("https://api.example.com/test")),
            Method = new Input<string>("GET"),
            ExpectedStatusCodes = new List<HttpStatusCodeCase>
            {
                new(200, mockActivity200)
            }
            // No UnmatchedStatusCode activity configured
        };

        var childActivities = new[] { mockActivity200 };
        var responseHandler = CreateResponseHandler(HttpStatusCode.InternalServerError); // 500 - no match

        // Act - Execute with child activities in workflow context
        var context = await ExecuteActivityWithChildActivitiesInBackgroundMode(
            sendHttpRequest, childActivities, responseHandler);

        // Assert - Verify that a null activity was scheduled (this is the correct behavior)
        // The SendHttpRequest always schedules something with a completion callback, even if the activity is null
        var scheduledActivities = context.GetBackgroundScheduledActivities().ToList();
        Assert.Single(scheduledActivities);
        
        var scheduledActivity = scheduledActivities.First();
        Assert.Null(scheduledActivity.ActivityNodeId); // No activity to schedule, so NodeId is null
        Assert.Equal("OnChildActivityCompletedAsync", scheduledActivity.Options?.CompletionCallback); // But still has completion callback
    }

    [Theory]
    [InlineData("FailedToConnect")]
    [InlineData("Timeout")]
    public async Task Should_Schedule_Error_Handling_Activity_On_Exception(string errorType)
    {
        // Arrange
        var mockFailedToConnectActivity = CreateMockActivity();
        var mockTimeoutActivity = CreateMockActivity();
        
        var sendHttpRequest = new SendHttpRequest
        {
            Url = new Input<Uri?>(new Uri("https://api.example.com/error")),
            Method = new Input<string>("GET"),
            ExpectedStatusCodes = new List<HttpStatusCodeCase>(),
            FailedToConnect = mockFailedToConnectActivity,
            Timeout = mockTimeoutActivity
        };

        var childActivities = new[] { mockFailedToConnectActivity, mockTimeoutActivity };

        // Create a response handler that throws the appropriate exception
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseHandler = errorType switch
        {
            "FailedToConnect" => (_, _) => throw new HttpRequestException("Connection failed"),
            "Timeout" => (_, _) => throw new TaskCanceledException("Request timed out"),
            _ => throw new ArgumentException($"Unknown error type: {errorType}")
        };

        // Act - Execute with child activities in workflow context
        var context = await ExecuteActivityWithChildActivitiesInBackgroundMode(
            sendHttpRequest, childActivities, responseHandler);

        // Assert - Verify the correct error handling activity was scheduled
        var scheduledActivities = context.GetBackgroundScheduledActivities().ToList();
        Assert.Single(scheduledActivities);
        
        var scheduledActivity = scheduledActivities.First();
        var expectedActivity = errorType switch
        {
            "FailedToConnect" => mockFailedToConnectActivity,
            "Timeout" => mockTimeoutActivity,
            _ => throw new ArgumentException($"Unknown error type: {errorType}")
        };
        
        var expectedNode = context.WorkflowExecutionContext.FindNodeByActivity(expectedActivity);
        Assert.Equal(expectedNode?.NodeId, scheduledActivity.ActivityNodeId);
    }

    // Keep the original property configuration test but rename it to be clear about what it tests
    [Theory]
    [InlineData("FailedToConnect", "https://unreachable.example.com")]
    [InlineData("Timeout", "https://slow.example.com")]
    [InlineData("UnmatchedStatusCode", "https://api.example.com/error")]
    public Task Should_Allow_Configuration_Of_Error_Handling_Activities(string activityType, string url)
    {
        // Arrange
        var mockActivity = CreateMockActivity();
        var sendHttpRequest = CreateSendHttpRequestWithErrorHandling(url, activityType, mockActivity);

        // Act & Assert - This tests property configuration, not scheduling behavior
        var configuredActivity = activityType switch
        {
            "FailedToConnect" => sendHttpRequest.FailedToConnect,
            "Timeout" => sendHttpRequest.Timeout,
            "UnmatchedStatusCode" => sendHttpRequest.UnmatchedStatusCode,
            _ => throw new ArgumentException($"Unknown activity type: {activityType}")
        };

        Assert.Equal(mockActivity, configuredActivity);
        return Task.CompletedTask;
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

        var responseHandler = CreateResponseHandler(HttpStatusCode.OK, additionalHeaders: expectedHeaders);
        var sendHttpRequest = CreateSendHttpRequest(new Uri("https://api.example.com/headers"));

        // Act
        var context = await ExecuteActivityWithResponseHandler(sendHttpRequest, responseHandler);

        // Assert
        var responseHeadersObj = context.GetExecutionOutput(_ => sendHttpRequest.ResponseHeaders);
        var responseHeaders = responseHeadersObj as HttpHeaders;
        Assert.NotNull(responseHeaders);
        Assert.True(responseHeaders.ContainsKey("Custom-Header"));
        Assert.True(responseHeaders.ContainsKey("X-Rate-Limit"));
    }

    [Fact]
    public void Should_Have_Correct_Activity_Attributes_And_HttpStatusCodeCase_Creation()
    {
        // Test activity attributes
        ActivityTestHelper.AssertActivityAttributes(
            typeof(SendHttpRequest),
            expectedNamespace: "Elsa",
            expectedCategory: "HTTP", 
            expectedDisplayName: "HTTP Request",
            expectedDescription: "Send an HTTP request.",
            expectedKind: ActivityKind.Task
        );

        // Test HttpStatusCodeCase creation
        const int statusCode = 200;
        var mockActivity = CreateMockActivity();
        var httpStatusCodeCase = new HttpStatusCodeCase(statusCode, mockActivity);

        Assert.Equal(statusCode, httpStatusCodeCase.StatusCode);
        Assert.Equal(mockActivity, httpStatusCodeCase.Activity);
    }

    // Private helper methods placed after all public members
    private static IActivity CreateMockActivity() => Substitute.For<IActivity>();

    private static Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> CreateResponseHandler(
        HttpStatusCode statusCode, 
        string? content = null,
        RequestCapture? requestCapture = null,
        Dictionary<string, string>? additionalHeaders = null)
    {
        return (HttpRequestMessage request, CancellationToken _) =>
        {
            if (requestCapture != null)
                requestCapture.CapturedRequest = request;
            return Task.FromResult(ActivityTestHelper.CreateHttpResponse(statusCode, content, additionalHeaders));
        };
    }

    private sealed class RequestCapture
    {
        public HttpRequestMessage? CapturedRequest { get; set; }
    }

    private static async Task<ActivityExecutionContext> ExecuteActivityWithResponseHandler(
        SendHttpRequest sendHttpRequest,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseHandler)
    {
        return await ActivityTestHelper.ExecuteActivityAsync(sendHttpRequest, 
            services => ActivityTestHelper.ConfigureHttpActivityServices(services, responseHandler));
    }

    private static async Task<ActivityExecutionContext> ExecuteActivityWithChildActivitiesInBackgroundMode(
        SendHttpRequest sendHttpRequest,
        IEnumerable<IActivity> childActivities,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseHandler)
    {
        return await ActivityTestHelper.ExecuteActivityAsync(
            sendHttpRequest, 
            services => ActivityTestHelper.ConfigureHttpActivityServices(services, responseHandler),
            childActivities: childActivities);
    }

    private static SendHttpRequest CreateSendHttpRequest(
        Uri url,
        string method = "GET",
        object? content = null,
        string? contentType = null,
        string? authorization = null)
    {
        return new SendHttpRequest
        {
            Url = new Input<Uri?>(url),
            Method = new Input<string>(method),
            Content = content != null ? new Input<object?>(content, null) : null,
            ContentType = contentType != null ? new Input<string?>(contentType, null) : null,
            Authorization = authorization != null ? new Input<string?>(authorization, null) : null,
            ExpectedStatusCodes = new List<HttpStatusCodeCase>()
        };
    }

    private static SendHttpRequest CreateSendHttpRequestWithErrorHandling(
        string url, 
        string activityType, 
        IActivity activity)
    {
        var request = new SendHttpRequest
        {
            Url = new Input<Uri?>(new Uri(url)),
            Method = new Input<string>("GET"),
            ExpectedStatusCodes = new List<HttpStatusCodeCase>()
        };

        switch (activityType)
        {
            case "FailedToConnect":
                request.FailedToConnect = activity;
                break;
            case "Timeout":
                request.Timeout = activity;
                break;
            case "UnmatchedStatusCode":
                request.UnmatchedStatusCode = activity;
                break;
        }

        return request;
    }

    private static SendHttpRequest CreateSendHttpRequestWithStatusCodes(
        string url, 
        List<HttpStatusCodeCase> statusCodes)
    {
        return new SendHttpRequest
        {
            Url = new Input<Uri?>(new Uri(url)),
            Method = new Input<string>("GET"),
            ExpectedStatusCodes = statusCodes
        };
    }
}
