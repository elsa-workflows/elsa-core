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

        [ActivityProperty(Hint = "The ID assigned to the blob.")]
        [Required]
        public string BlobID { get; set; }
        [ActivityProperty(Hint = "The bytes")]
        public byte[] Bytes { get; set; }
        [ActivityProperty(Hint = "The output file path, alternative to Bytes")]
        public string FilePath { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(BlobID))
                throw new System.Exception($"BlobID must have a value");
            if (!string.IsNullOrWhiteSpace(FilePath))
                await _storage.ReadToFileAsync(BlobID, FilePath, context.CancellationToken);
            else
                Bytes = await _storage.ReadBytesAsync(BlobID);
            return Done();
        }
    }
}