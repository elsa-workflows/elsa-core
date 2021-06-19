using System.ComponentModel.DataAnnotations;
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
        Description = "Reads a blob from the storage engine.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ReadBlob : Activity
    {
        private readonly IBlobStorage _storage;
        public ReadBlob(IBlobStorage storage) => _storage = storage;

        [ActivityInput(Hint = "The Id assigned to the blob.")]
        [Required]
        public string BlobId { get; set; } = default!;

        [ActivityInput(Hint = "If set, the output of this activity is written to the specified file. Otherwise, the bytes of the blob will be set as the activity output.")]
        public string? DestinationFilePath { get; set; }

        [ActivityOutput] public byte[]? Output { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(BlobId))
                throw new System.Exception($"{nameof(BlobId)} must have a value");

            if (string.IsNullOrWhiteSpace(DestinationFilePath))
                Output = await _storage.ReadBytesAsync(BlobId, context.CancellationToken);
            else
                await _storage.ReadToFileAsync(BlobId, DestinationFilePath, context.CancellationToken);

            return Done();
        }
    }
}