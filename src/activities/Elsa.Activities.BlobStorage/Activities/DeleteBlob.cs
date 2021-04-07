using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using Storage.Net.Blobs;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.BlobStorage
{
    /// <summary>
    /// Writes a text string to the console.
    /// </summary>
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

        [ActivityProperty(Hint = "The ID of the blob")]
        public string BlobID { get; set; }
        [ActivityProperty(Hint = "The IDs of the blob")]
        public List<string> BlobIDs { get; set; } = new List<string>();

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(BlobID) && (BlobIDs==default || !BlobIDs.Any()))
                throw new System.Exception($"BlobID or BlobIds must have a value");
            if (BlobID != default)
                BlobIDs.Add(BlobID);
            await _storage.DeleteAsync(BlobIDs);
            return Done();
        }
    }
}