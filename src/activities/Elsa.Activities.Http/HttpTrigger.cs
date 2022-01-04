using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Management.Models;
using Elsa.Models;

namespace Elsa.Activities.Http;

public class HttpTrigger : Trigger
{
    [Input] public Input<string> Path { get; set; } = default!;

    [Input(
        Options = new[] { "GET", "POST", "PUT" },
        UIHint = InputUIHints.CheckList
    )]
    public Input<ICollection<string>> SupportedMethods { get; set; } = new(new[] { HttpMethod.Get.Method });

    [Output] public Output<HttpRequestModel>? Result { get; set; }

    protected override IEnumerable<object> GetHashInputs(TriggerIndexingContext context)
    {
        var path = context.ExpressionExecutionContext.Get(Path);
        var methods = context.ExpressionExecutionContext.Get(SupportedMethods);
        return methods!.Select(x => (path!.ToLowerInvariant(), x.ToLowerInvariant())).Cast<object>().ToArray();
    }

    protected override void Execute(ActivityExecutionContext context)
    {
        var bookmarks = CreateBookmarks(context).ToList();
        context.SetBookmarks(bookmarks);
    }

    private IEnumerable<Bookmark> CreateBookmarks(ActivityExecutionContext context)
    {
        var path = context.Get(Path)!;
        var methods = context.Get(SupportedMethods)!;
        var hasher = context.GetRequiredService<IHasher>();
        var identityGenerator = context.GetRequiredService<IIdentityGenerator>();

        foreach (var method in methods)
        {
            var hashInput = (path.ToLowerInvariant(), method.ToLowerInvariant());
            var hash = hasher.Hash(hashInput);
            var bookmarkId = identityGenerator.GenerateId();
            yield return new Bookmark(bookmarkId, NodeType, hash, Id, context.Id);
        }
    }
}