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
        Description = "Write a blob to the storage engine",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class WriteBlob : Activity
    {
        public WriteBlob(IBlobStorage storage)
        {
            _storage = storage;
        }
        private readonly IBlobStorage _storage;

        [ActivityProperty(Hint = "The ID to be assigned to the blob. It's needed to retrieve the blob")]
        [Required]
        public string BlobID { get; set; }
        [ActivityProperty(Hint = "The bytes")]
        public byte[] Bytes { get; set; }
        [ActivityProperty(Hint = "The file path")]
        public string FilePath { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(BlobID))
                throw new System.Exception($"BlobID must have a value");
            if (!string.IsNullOrWhiteSpace(FilePath))
                await _storage.WriteFileAsync(BlobID, FilePath, context.CancellationToken);
            if(Bytes!=default && Bytes.Any())
                await _storage.WriteAsync(BlobID, new MemoryStream(Bytes), default, context.CancellationToken);
            return Done();
        }
    }
}