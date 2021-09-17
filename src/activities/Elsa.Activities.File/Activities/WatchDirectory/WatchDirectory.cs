using Elsa.Activities.File.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Services;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
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
        [Required]
        [ActivityInput(Hint = "The path of the directory to watch")]
        public string? Path { get; set; }

        [ActivityInput(Hint = "The file pattern for interested files")]
        public string? Pattern { get; set; }

        [ActivityInput(Label = "Change Types", Hint = "The types of file system events to subscribe to", UIHint = ActivityInputUIHints.CheckList, DefaultValue = WatcherChangeTypes.All)]
        public WatcherChangeTypes ChangeTypes { get; set; }

        [ActivityInput(Label = "Notify Filters", UIHint = ActivityInputUIHints.CheckList, DefaultValue = NotifyFilters.Attributes |
            NotifyFilters.CreationTime |
            NotifyFilters.DirectoryName |
            NotifyFilters.FileName |
            NotifyFilters.LastAccess |
            NotifyFilters.LastWrite |
            NotifyFilters.Security |
            NotifyFilters.Size)]
        public NotifyFilters NotifyFilters { get; set; }

        [ActivityOutput]
        public FileSystemEvent? Output { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var model = (FileSystemEvent)context.Input!;
            Output = model;
            return Done(Output);
        }
    }
}
