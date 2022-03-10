using Elsa.Activities.Console;
using Elsa.Activities.File;
using Elsa.Activities.File.Models;
using Elsa.Builders;
using System;
using System.IO;
using System.Text;

namespace Elsa.Samples.WatchDirectoryWorker.Workflows
{
    public class WatchDirectoryCreatedWorkflow : IWorkflow
    {
        private readonly string _directory;
        private readonly string _systemRoot = Path.GetPathRoot(Environment.SystemDirectory);

        public WatchDirectoryCreatedWorkflow(string directory)
        {
            _directory = directory;
        }

        public void Build(IWorkflowBuilder builder) => builder.WatchDirectory(setup => 
            {
                setup.WithPath(Path.Combine(_systemRoot, "Temp\\FileWatchers"))
                    .WithPattern("*.txt")
                    .WithChangeTypes(WatcherChangeTypes.Created);
            })
            .ReadFile(setup =>
            {
                setup.WithPath(context => context.GetInput<FileSystemEvent>().FullPath);
            })
            .WriteLine(setup =>
            {
                setup.WithText(async context =>
                {
                    var fsEvent = await context.WorkflowExecutionContext.GetActivityPropertyAsync<WatchDirectory, FileSystemEvent>("activity-1", a => a.Output);
                    var data = await context.WorkflowExecutionContext.GetActivityPropertyAsync<ReadFile, byte[]>("activity-2", a => a.Bytes);
                    var line = $"{GetType().Name}-{fsEvent.FullPath}-{fsEvent.ChangeType}-{Encoding.UTF8.GetString(data)}";
                    return line;
                });
            })
            .DeleteFile(context => context.GetInput<FileSystemEvent>().FullPath);
    }
}
