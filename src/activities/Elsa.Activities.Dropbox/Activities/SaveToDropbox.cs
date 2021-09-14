using System.Threading.Tasks;
using Elsa.Activities.Dropbox.Models;
using Elsa.Activities.Dropbox.Services;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
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

        [ActivityInput(Hint = "The file to store.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public byte[] FileData { get; set; } = default!;

        [ActivityInput(Hint = "The path to which the file should be saved.", SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid })]
        public string Path { get; set; } = default!;

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            var response = await _filesApi.UploadAsync(
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

            context.JournalData.Add("Response", response);
            return Done();
        }
    }
}