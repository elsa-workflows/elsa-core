using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Elsa.Attributes;
using Elsa.Management.Models;
using Elsa.Models;
using Elsa.Modules.Http.Models;

namespace Elsa.Modules.Http;

[Activity("Elsa", "HTTP", "Waits for an inbound HTTP request that matches the specified path and methods")]
public class HttpEndpoint : Trigger<HttpRequestModel>
{
    public const string InputKey = "HttpRequest";

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

    protected override IEnumerable<object> GetTriggerData(TriggerIndexingContext context) => GetBookmarkData(context.ExpressionExecutionContext);

    protected override void Execute(ActivityExecutionContext context)
    {
        // If we did not receive external input, it means we are just now encountering this activity.
        if (!context.TryGetInput<HttpRequestModel>(InputKey, out var request))
        {
            // Create bookmarks for when we receive the expected HTTP request.
            context.CreateBookmarks(GetBookmarkData(context.ExpressionExecutionContext));
            return;
        }

        // Provide the received HTTP request as output.
        context.Set(Result, request);
    }

    private IEnumerable<object> GetBookmarkData(ExpressionExecutionContext context)
    {
        // Generate bookmark data for path and selected methods.
        var path = context.Get(Path);
        var methods = context.Get(SupportedMethods);
        return methods!.Select(x => new HttpBookmarkData(path!, x.ToLowerInvariant())).Cast<object>().ToArray();
    }
}