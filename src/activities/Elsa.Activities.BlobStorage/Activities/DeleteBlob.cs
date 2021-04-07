using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class DeleteBlob : Activity
    {
        public DeleteBlob(IBlobStorage storage)
        {
            _storage = storage;
        }
        private readonly IBlobStorage _storage;

        [ActivityProperty(Hint = "The Ids of the blobs", 
            UIHint = ActivityPropertyUIHints.MultiText, 
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public IList<string> BlobIds { get; set; } = new List<string>();

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (BlobIds==default || !BlobIds.Any())
                throw new System.Exception($"BlobID or BlobIds must have a value");            
            await _storage.DeleteAsync(BlobIds);
            return Done();
        }
    }
}