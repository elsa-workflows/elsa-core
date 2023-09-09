using System.Linq;
using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Conductor
{
    [Job(
        Category = "Conductor",
        Description = "Sends a task to your application and waits for the application to report the task as completed or cancelled."
    )]
    public class RunTask : Activity
    {
        private readonly IEventPublisher _eventPublisher;

        public RunTask(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }
        
        [ActivityInput(
            Label = "Run Task",
            Hint = "The task to run.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string TaskName { get; set; } = default!;

        [ActivityInput(
            Hint = "Any input to send along with the task to your application.",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public object? Payload { get; set; }
        
        [ActivityOutput(Hint = "Any input that was received along with the task completion.")]
        public object? Output { get; set; }

        protected override async ValueTask<IActivityExecutionResult> OnExecuteAsync(ActivityExecutionContext context)
        {
            await _eventPublisher.PublishAsync(new RunTaskModel(TaskName, Payload, context.WorkflowInstance.Id));
            return Suspend();
        }

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context)
        {
            var eventModel = context.GetInput<TaskResultModel>()!;
            var outcomes = eventModel.Outcomes;

            if (outcomes?.Any() == false)
                outcomes = new[] { OutcomeNames.Done };

            Output = eventModel.Payload;
            context.LogOutputProperty(this, nameof(Output), Output);
            return base.Outcomes(outcomes!);
        }
    }
}