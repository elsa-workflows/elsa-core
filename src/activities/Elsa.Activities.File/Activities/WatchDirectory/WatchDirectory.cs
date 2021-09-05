using Elsa.Activities.File.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.File
{
    [Trigger(Category = "File",
        Description = "Triggers when files are created/modified in the given folder",
        Outcomes = new[] { OutcomeNames.Done })]
    public class WatchDirectory : Activity
    {
        [ActivityInput(Hint = "The path of the directory to watch")]
        public string Path { get; set; }

        [ActivityInput(Hint = "The file pattern for interested files")]
        public string Pattern { get; set; }

        [ActivityOutput]
        public FileSystemEvent? Output { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var model = (FileSystemEvent)context.Input!;
            Output = model;
            return Done();
        }
    }
}
