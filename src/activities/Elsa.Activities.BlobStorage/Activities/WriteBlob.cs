using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using FluentStorage.Storage;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.BlobStorage
{
    [Action(
        Category = "BlobStorage",
        Description = "Write a blob to the storage engine.",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class WriteBlob : Activity
    {
        public WriteBlob(IStore storage)
        {
            _storage = storage;
        }

        private readonly IStore _storage;

        [ActivityInput(Hint = "The ID to be assigned to the blob.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        [Required]
        public string BlobId { get; set; } = default!;

        [ActivityInput(Hint = "The bytes to write.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript }, DefaultSyntax = SyntaxNames.JavaScript)]
        public byte[]? Bytes { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(BlobId))
                throw new System.Exception($"{nameof(BlobId)} must have a value");

            if (Bytes != default && Bytes.Any())
                await _storage.SetBytes(BlobId, Bytes, default, context.CancellationToken); // original impl used 'new MemoryStrem(Bytes)' changed to SetBytes to allow the IStore impl to decide
            
            return Done();
        }
    }
}