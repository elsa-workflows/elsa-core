using System.Threading.Tasks;
using Elsa.Activities.Dropbox.Models;
using Elsa.Activities.Dropbox.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Dropbox.Activities
{
    [Action(
        Category = "Dropbox",
        Description = "Saves a given file or byte array to a folder in Dropbox",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class SaveToDropbox : Activity
    {
        private readonly IFilesApi _filesApi;

        public SaveToDropbox(IFilesApi filesApi)
        {
            _filesApi = filesApi;
        }

        [ActivityProperty(Hint = "An expression evaluating to a byte array to store.")]
        public byte[] FileData { get; set; } = default!;

        [ActivityProperty(Hint = "An expression evaluating to the path to which the file should be saved.")]
        public string Path { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _filesApi.UploadAsync(
                new UploadRequest
                {
                    Mode = new UploadMode
                    {
                        Tag = UploadModeUnion.Overwrite
                    },
                    Path = Path,
                },
                FileData,
                context.CancellationToken
            );

            return Done();
        }
    }
}