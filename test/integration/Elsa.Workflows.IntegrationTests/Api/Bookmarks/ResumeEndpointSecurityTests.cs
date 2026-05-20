using System.Net;
using System.Text;
using Elsa.SasTokens.Contracts;
using Elsa.Workflows.Api.Endpoints.Bookmarks.Resume;
using Elsa.Workflows.Runtime;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace Elsa.Workflows.IntegrationTests.Api.Bookmarks;

public class ResumeEndpointSecurityTests
{
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly IPayloadSerializer _payloadSerializer = Substitute.For<IPayloadSerializer>();
    private readonly IApiSerializer _apiSerializer = Substitute.For<IApiSerializer>();
    private readonly IWorkflowResumer _workflowResumer = Substitute.For<IWorkflowResumer>();
    private readonly IBookmarkQueue _bookmarkQueue = Substitute.For<IBookmarkQueue>();

    public ResumeEndpointSecurityTests()
    {
        _tokenService.TryDecryptToken<BookmarkTokenPayload>(Arg.Any<string>(), out Arg.Any<BookmarkTokenPayload>())
            .Returns(callInfo =>
            {
                callInfo[1] = new BookmarkTokenPayload("bookmark", "workflow");
                return false;
            });
    }

    [Fact]
    public async Task Get_WithInvalidToken_DoesNotParseQueryInput()
    {
        var input = Uri.EscapeDataString("{not-json");
        var context = CreateHttpContext(HttpMethods.Get, $"?t=invalid&async=true&in={input}");
        var sut = CreateEndpoint(context);

        await sut.HandleAsync(CancellationToken.None);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        _payloadSerializer.Received(0).Deserialize<IDictionary<string, object>>(Arg.Any<string>());
        _apiSerializer.Received(0).Deserialize<Request>(Arg.Any<string>());
        await _workflowResumer.DidNotReceive().ResumeAsync(Arg.Any<ResumeBookmarkRequest>(), Arg.Any<CancellationToken>());
        await _bookmarkQueue.DidNotReceive().EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("GET", "?async=true&in=%7Bnot-json", null)]
    [InlineData("POST", "?async=true", """{"input":{"value":"ignored"}}""")]
    public async Task Request_WithMissingToken_DoesNotParseInput(string method, string queryString, string? body)
    {
        var context = CreateHttpContext(method, queryString, body);
        var sut = CreateEndpoint(context);

        await sut.HandleAsync(CancellationToken.None);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        _apiSerializer.Received(0).Deserialize<Request>(Arg.Any<string>());
        _payloadSerializer.Received(0).Deserialize<IDictionary<string, object>>(Arg.Any<string>());
        await _workflowResumer.DidNotReceive().ResumeAsync(Arg.Any<ResumeBookmarkRequest>(), Arg.Any<CancellationToken>());
        await _bookmarkQueue.DidNotReceive().EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_WithInvalidToken_DoesNotParseBodyInput()
    {
        var context = CreateHttpContext(HttpMethods.Post, "?t=invalid", """{"input":{"value":"ignored"}}""");
        var sut = CreateEndpoint(context);

        await sut.HandleAsync(CancellationToken.None);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        _apiSerializer.Received(0).Deserialize<Request>(Arg.Any<string>());
        _payloadSerializer.Received(0).Deserialize<IDictionary<string, object>>(Arg.Any<string>());
        await _workflowResumer.DidNotReceive().ResumeAsync(Arg.Any<ResumeBookmarkRequest>(), Arg.Any<CancellationToken>());
        await _bookmarkQueue.DidNotReceive().EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Post_WithJsonNullBody_ReturnsBadRequest()
    {
        var context = CreateHttpContext(HttpMethods.Post, "?t=valid", "null");
        var sut = CreateEndpoint(context);
        _tokenService.TryDecryptToken<BookmarkTokenPayload>("valid", out Arg.Any<BookmarkTokenPayload>())
            .Returns(callInfo =>
            {
                callInfo[1] = new BookmarkTokenPayload("bookmark", "workflow");
                return true;
            });
        _apiSerializer.Deserialize<Request>("null").Returns((Request?)null);

        await sut.HandleAsync(CancellationToken.None);

        Assert.Equal((int)HttpStatusCode.BadRequest, context.Response.StatusCode);
        await _workflowResumer.DidNotReceive().ResumeAsync(Arg.Any<ResumeBookmarkRequest>(), Arg.Any<CancellationToken>());
        await _bookmarkQueue.DidNotReceive().EnqueueAsync(Arg.Any<NewBookmarkQueueItem>(), Arg.Any<CancellationToken>());
    }

    private Resume CreateEndpoint(DefaultHttpContext context)
    {
        return Factory.Create<Resume>(context, _tokenService, _workflowResumer, _bookmarkQueue, _payloadSerializer, _apiSerializer);
    }

    private static DefaultHttpContext CreateHttpContext(string method, string queryString, string? body = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.QueryString = new QueryString(queryString);
        context.Response.Body = new MemoryStream();

        if (body == null)
            return context;

        var bodyBytes = Encoding.UTF8.GetBytes(body);
        context.Request.Body = new MemoryStream(bodyBytes);
        context.Request.ContentLength = bodyBytes.Length;
        context.Request.ContentType = "application/json";
        return context;
    }
}
