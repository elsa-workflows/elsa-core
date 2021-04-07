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
    [Action(
        Category = "BlobStorage",
        Description = "Reads a blob from the storage engine",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ReadBlob : Activity
    {
        public ReadBlob(IBlobStorage storage)
        {
            _storage = storage;
        }
        private readonly IBlobStorage _storage;

        [ActivityProperty(Hint = "The Id assigned to the blob.")]
        [Required]
        public string BlobId { get; set; }

        [ActivityProperty(Hint = "If set, the output of this activity is written into the specified file, alternatively the outcome contains the bytes of the blob")]
        public string FilePath { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(BlobId))
                throw new System.Exception($"BlobId must have a value");
            if (!string.IsNullOrWhiteSpace(FilePath))
                await _storage.ReadToFileAsync(BlobId, FilePath, context.CancellationToken);
            else
            {
                Done(await _storage.ReadBytesAsync(BlobId));
            }
            return Done();
        }
    }
}