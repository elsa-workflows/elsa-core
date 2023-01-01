﻿using System.Text.Json.Serialization;
using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Http.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Management.Models;

namespace Elsa.Http;

[Activity("Elsa", "HTTP", "Wait for an inbound HTTP request that matches the specified path and methods.")]
public class HttpEndpoint : Trigger<HttpRequestModel>
{
    public const string InputKey = "HttpRequest";

    [JsonConstructor]
    public HttpEndpoint()
    {
    }

    [Input] public Input<string> Path { get; set; } = default!;

    [Input(
        Options = new[] { "GET", "POST", "PUT", "HEAD", "DELETE" },
        UIHint = InputUIHints.CheckList
    )]
    public Input<ICollection<string>> SupportedMethods { get; set; } = new(new[] { HttpMethod.Get.Method });

    [Input(
        Description = "Allow authenticated requests only",
        Category = "Security"
    )]
    public Input<bool> Authorize { get; set; } = new(false);

    [Input(
        Description = "Provide a policy to evaluate. If the policy fails, the request is forbidden.",
        Category = "Security"
    )]
    public Input<string?> Policy { get; set; } = new(default(string?));

    /// <inheritdoc />
    protected override IEnumerable<object> GetTriggerPayloads(TriggerIndexingContext context) => GetBookmarkPayloads(context.ExpressionExecutionContext);

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        // If we did not receive external input, it means we are just now encountering this activity and we need to block execution by creating a bookmark.
        if (!context.TryGetInput<HttpRequestModel>(InputKey, out var request))
        {
            // Create bookmarks for when we receive the expected HTTP request.
            context.CreateBookmarks(GetBookmarkPayloads(context.ExpressionExecutionContext));
            return;
        }

        // Provide the received HTTP request as output.
        context.Set(Result, request);
        
        // Complete.
        await context.CompleteActivityAsync();
    }

    private IEnumerable<object> GetBookmarkPayloads(ExpressionExecutionContext context)
    {
        // Generate bookmark data for path and selected methods.
        var path = context.Get(Path);
        var methods = context.Get(SupportedMethods);
        return methods!.Select(x => new HttpEndpointBookmarkPayload(path!, x.ToLowerInvariant())).Cast<object>().ToArray();
    }
}