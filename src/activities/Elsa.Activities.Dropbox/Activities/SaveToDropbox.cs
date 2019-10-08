using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Dropbox.Models;
using Elsa.Activities.Dropbox.Services;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Extensions;
using Elsa.Results;
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
        private readonly IWorkflowExpressionEvaluator expressionEvaluator;

        public SaveToDropbox(IFilesApi filesApi, IWorkflowExpressionEvaluator expressionEvaluator)
        {
            this.filesApi = filesApi;
            this.expressionEvaluator = expressionEvaluator;
        }

        [ActivityProperty(Hint = "An expression evaluating to a byte array to store.")]
        public WorkflowExpression<byte[]> DataExpression
        {
            get => GetState<WorkflowExpression<byte[]>>();
            set => SetState(value);
        }

        [ActivityProperty(Hint = "An expression evaluating to the path to which the file should be saved.")]
        public WorkflowExpression<string> PathExpression
        {
            get => GetState<WorkflowExpression<string>>();
            set => SetState(value);
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {
            var data = await expressionEvaluator.EvaluateAsync(DataExpression, context, cancellationToken);
            var path = await expressionEvaluator.EvaluateAsync(PathExpression, context, cancellationToken);

            await filesApi.UploadAsync(
                new UploadRequest
                {
                    Mode = new UploadMode
                    {
                        Tag = UploadModeUnion.Overwrite
                    },
                    Path = path,
                },
                data,
                cancellationToken
            );

            return Done();
        }
    }
}