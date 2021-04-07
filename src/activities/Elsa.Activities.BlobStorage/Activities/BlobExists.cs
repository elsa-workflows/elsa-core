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
        Outcomes = new[] { OutcomeNames.True, OutcomeNames.False }
    )]
    public class BlobExists : Activity
    {
        public BlobExists(IBlobStorage storage)
        {
            _storage = storage;
        }
        private readonly IBlobStorage _storage;

        [ActivityProperty(Hint = "The ID of the blob")]
        public string BlobID { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(BlobID))
                throw new System.Exception($"BlobID must have a value");
            if (await _storage.ExistsAsync(BlobID))
                return Outcome(OutcomeNames.True);
            else
                return Outcome(OutcomeNames.False);
        }
    }
}