using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Dropbox.Models;
using Elsa.Activities.Dropbox.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Activities.Dropbox.Activities
{
    [ActivityDefinition(
        Category = "Dropbox",
        Description = "Saves a given file or byte array to a folder in Dropbox",
        Icon = "fab fa-dropbox")]
    public class SaveToDropbox : Activity
    {
        private readonly IFilesApi filesApi;

        public SaveToDropbox(IFilesApi filesApi)
        {
            this.filesApi = filesApi;
        }

        [ActivityProperty(Hint = "An expression evaluating to a byte array to store.")]
        public byte[] Data { get; set; }

        [ActivityProperty(Hint = "An expression evaluating to the path to which the file should be saved.")]
        public string Path { get; set; }

        protected override async Task<IActivityExecutionResult> OnExecuteAsync(
            ActivityExecutionContext context,
            CancellationToken cancellationToken)
        {
            await filesApi.UploadAsync(
                new UploadRequest
                {
                    Mode = new UploadMode
                    {
                        Tag = UploadModeUnion.Overwrite
                    },
                    Path = Path,
                },
                Data,
                cancellationToken
            );

            return Done();
        }
    }
}