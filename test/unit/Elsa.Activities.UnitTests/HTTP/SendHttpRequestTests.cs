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

    [Fact]
    public async Task Should_Schedule_Matching_Activity_When_Status_Code_Has_Corresponding_Handler()
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
        var responseHandler = CreateResponseHandler(HttpStatusCode.NotFound); // 404

        // Act - Execute with child activities in workflow context
        var context = await ExecuteActivityWithChildActivitiesInBackgroundMode(
            sendHttpRequest, childActivities, responseHandler);

        // Assert - Get scheduled activities and verify correct one was scheduled
        var scheduledActivities = context.GetBackgroundScheduledActivities().ToList();
        Assert.Single(scheduledActivities);
        var scheduledActivity = scheduledActivities.First();
        var expectedNode = context.WorkflowExecutionContext.FindNodeByActivity(mockActivity404);
        Assert.Equal(expectedNode?.NodeId, scheduledActivity.ActivityNodeId);
    }

    [Fact]
    public async Task Should_Schedule_Unmatched_Activity_When_Status_Code_Has_No_Corresponding_Handler()
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
        var responseHandler = CreateResponseHandler(HttpStatusCode.InternalServerError); // 500 - no match

        // Act - Execute with child activities in workflow context
        var context = await ExecuteActivityWithChildActivitiesInBackgroundMode(
            sendHttpRequest, childActivities, responseHandler);

        // Assert - Get scheduled activities and verify correct one was scheduled
        var scheduledActivities = context.GetBackgroundScheduledActivities().ToList();
        Assert.Single(scheduledActivities);
        var scheduledActivity = scheduledActivities.First();
        var expectedNode = context.WorkflowExecutionContext.FindNodeByActivity(mockUnmatchedActivity);
        Assert.Equal(expectedNode?.NodeId, scheduledActivity.ActivityNodeId);
    }

    [Fact]
    public async Task Should_Schedule_FailedToConnect_Activity_On_HttpRequestException()
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
        
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseHandler = 
            (_, _) => throw new HttpRequestException("Connection failed");

        // Act - Execute with child activities in workflow context
        var context = await ExecuteActivityWithChildActivitiesInBackgroundMode(
            sendHttpRequest, childActivities, responseHandler);

        // Assert - Verify the FailedToConnect activity was scheduled
        var scheduledActivities = context.GetBackgroundScheduledActivities().ToList();
        Assert.Single(scheduledActivities);
        
        var scheduledActivity = scheduledActivities.First();
        var expectedNode = context.WorkflowExecutionContext.FindNodeByActivity(mockFailedToConnectActivity);
        Assert.Equal(expectedNode?.NodeId, scheduledActivity.ActivityNodeId);
    }

    [Fact]
    public async Task Should_Schedule_Timeout_Activity_On_TaskCanceledException()
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
        
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responseHandler = 
            (_, _) => throw new TaskCanceledException("Request timed out");

        // Act - Execute with child activities in workflow context
        var context = await ExecuteActivityWithChildActivitiesInBackgroundMode(
            sendHttpRequest, childActivities, responseHandler);

        // Assert - Verify the Timeout activity was scheduled
        var scheduledActivities = context.GetBackgroundScheduledActivities().ToList();
        Assert.Single(scheduledActivities);
        
        var scheduledActivity = scheduledActivities.First();
        var expectedNode = context.WorkflowExecutionContext.FindNodeByActivity(mockTimeoutActivity);
        Assert.Equal(expectedNode?.NodeId, scheduledActivity.ActivityNodeId);
    }

    [Fact]
    public void Should_Allow_Configuration_Of_FailedToConnect_Activity()
    {
        // Arrange
        var mockActivity = CreateMockActivity();
        var url = "https://unreachable.example.com";
        
        // Act
        var sendHttpRequest = new SendHttpRequest
        {
            Url = new Input<Uri?>(new Uri(url)),
            Method = new Input<string>("GET"),
            ExpectedStatusCodes = new List<HttpStatusCodeCase>(),
            FailedToConnect = mockActivity
        };

        // Assert - This tests property configuration
        Assert.Equal(mockActivity, sendHttpRequest.FailedToConnect);
    }

    [Fact]
    public void Should_Allow_Configuration_Of_Timeout_Activity()
    {
        // Arrange
        var mockActivity = CreateMockActivity();
        var url = "https://slow.example.com";
        
        // Act
        var sendHttpRequest = new SendHttpRequest
        {
            Url = new Input<Uri?>(new Uri(url)),
            Method = new Input<string>("GET"),
            ExpectedStatusCodes = new List<HttpStatusCodeCase>(),
            Timeout = mockActivity
        };

        // Assert - This tests property configuration
        Assert.Equal(mockActivity, sendHttpRequest.Timeout);
    }

    [Fact]
    public void Should_Allow_Configuration_Of_UnmatchedStatusCode_Activity()
    {
        // Arrange
        var mockActivity = CreateMockActivity();
        var url = "https://api.example.com/error";
        
        // Act
        var sendHttpRequest = new SendHttpRequest
        {
            Url = new Input<Uri?>(new Uri(url)),
            Method = new Input<string>("GET"),
            ExpectedStatusCodes = new List<HttpStatusCodeCase>(),
            UnmatchedStatusCode = mockActivity
        };

        // Assert - This tests property configuration
        Assert.Equal(mockActivity, sendHttpRequest.UnmatchedStatusCode);
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
}
