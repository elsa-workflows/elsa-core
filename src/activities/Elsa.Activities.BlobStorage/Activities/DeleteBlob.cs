using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Storage.Net.Blobs;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.BlobStorage
{
    [Action(
        Category = "BlobStorage", 
        Description = "Delete a blob from the storage engine.", 
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class DeleteBlob : Activity
    {
        private readonly IBlobStorage _storage;
        public DeleteBlob(IBlobStorage storage) => _storage = storage;

        [ActivityInput(
            Hint = "The IDs of the blobs.",
            UIHint = ActivityInputUIHints.MultiText,
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public IList<string> BlobIds { get; set; } = new List<string>();

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (BlobIds == default || !BlobIds.Any())
                throw new System.Exception($"BlobID or BlobIds must have a value");
            await _storage.DeleteAsync(BlobIds, context.CancellationToken);
            return Done();
        }
    }
}