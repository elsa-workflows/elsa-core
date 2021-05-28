using Elsa.Activities.File.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.File
{
    [Trigger(Category = "File",
        DisplayName = "Watch Directory",
        Description = "Watches a directory for file system changes",
        Outcomes = new[] { OutcomeNames.Done })]
    public class WatchDirectory : Activity
    {
        private ILogger _logger;

        public WatchDirectory(ILogger<WatchDirectory> logger)
        {
            _logger = logger;
        }

        public WatcherChangeTypes ChangeType { get; set; }

        public string Directory { get; set; }

        public string Pattern { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            return base.OnExecute(context);
        }

        public async override ValueTask<IActivityExecutionResult> ExecuteAsync(ActivityExecutionContext context)
        {
            var input = context.GetInput<FileSystemChanged>();
            _logger.LogInformation($"Directory={input.Directory}");
            return Done();
        }
    }
}
