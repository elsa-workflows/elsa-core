using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Elsa.Attributes;
using Elsa.Management.Models;
using Elsa.Models;
using Elsa.Modules.Http.Models;

namespace Elsa.Modules.Http;

public class HttpEndpoint : Trigger
{
    [Input] public Input<string> Path { get; set; } = default!;

    [Input(
        Options = new[] { "GET", "POST", "PUT" },
        UIHint = InputUIHints.CheckList
    )]
    public Input<ICollection<string>> SupportedMethods { get; set; } = new(new[] { HttpMethod.Get.Method });

    [Output] public Output<HttpRequestModel>? Result { get; set; }

    protected override IEnumerable<object> GetTriggerData(TriggerIndexingContext context) => GetBookmarkData(context.ExpressionExecutionContext);
    protected override void Execute(ActivityExecutionContext context) => context.SetBookmarks(GetBookmarkData(context.ExpressionExecutionContext));

    private IEnumerable<object> GetBookmarkData(ExpressionExecutionContext context)
    {
        // Generate bookmark data for path and selected methods.
        var path = context.Get(Path);
        var methods = context.Get(SupportedMethods);
        return methods!.Select(x => new HttpBookmarkData(path!, x.ToLowerInvariant())).Cast<object>().ToArray();
    }
}