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
    [Action(
        Category = "BlobStorage",
        Outcomes = new[] { OutcomeNames.True, OutcomeNames.False }
    )]
    public class BlobExists : Activity
    {
        public BlobExists(IBlobStorage storage)
        {
            _storage = storage;
        }
        private readonly IBlobStorage _storage;

        [ActivityProperty(Hint = "The Id of the blob")]
        public string BlobId { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(BlobId))
                throw new System.Exception($"BlobId must have a value");
            if (await _storage.ExistsAsync(BlobId))
                return Outcome(OutcomeNames.True);
            else
                return Outcome(OutcomeNames.False);
        }
    }
}