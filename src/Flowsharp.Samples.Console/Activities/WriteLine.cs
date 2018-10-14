using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flowsharp.ActivityResults;
using Flowsharp.Models;
using Activity = Flowsharp.Activities.Activity;

namespace Flowsharp.Samples.Console.Activities 
{
    /// <summary>
    /// Writes a text string to the specified stream.
    /// </summary>
    public class WriteLine : Activity
    {
        private readonly TextWriter output;
        private readonly Func<WorkflowExecutionContext, ActivityExecutionContext, string> textProvider;
        
        public WriteLine() : this(System.Console.Out, null)
        {
        }

        public WriteLine(string text) : this(System.Console.Out, text)
        {
        }
        
        public WriteLine(Func<WorkflowExecutionContext, ActivityExecutionContext, string> textProvider) : this(System.Console.Out, null)
        {
            this.textProvider = textProvider;
        }

        public WriteLine(TextWriter output, string text)
        {
            this.output = output;
            Text = text;
            textProvider = (w, a) => Text;
        }
        
        public string Text { get; set; }

        protected override bool CanExecute(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext)
        {
            var text = textProvider(workflowContext, activityContext);
            return text != null;
        }

        public override async Task<ActivityExecutionResult> ExecuteAsync(WorkflowExecutionContext workflowContext, ActivityExecutionContext activityContext, CancellationToken cancellationToken)
        {
            var text = textProvider(workflowContext, activityContext);
            await output.WriteLineAsync(text);
            return ActivateEndpoint();
        }
    }
}
