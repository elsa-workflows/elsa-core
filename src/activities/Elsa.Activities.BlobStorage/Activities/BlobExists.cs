using System;
using System.Threading.Tasks;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;
using Storage.Net.Blobs;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.BlobStorage
{
    [Action(
        Category = "BlobStorage",
        Description = "Check if a blob exists on the storage engine.",
        Outcomes = new[] { OutcomeNames.True, OutcomeNames.False }
    )]
    public class BlobExists : Activity
    {
        private readonly IBlobStorage _storage;
        public BlobExists(IBlobStorage storage) => _storage = storage;

        [ActivityInput(Hint = "The ID of the blob.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string BlobId { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(BlobId))
                throw new Exception($"{nameof(BlobId)} must have a value");

            if (await _storage.ExistsAsync(BlobId))
                return Outcome(OutcomeNames.True);

            return Outcome(OutcomeNames.False);
        }
    }
}