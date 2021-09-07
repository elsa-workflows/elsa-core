using Elsa.Activities.Console;
using Elsa.Activities.File;
using Elsa.Activities.File.Models;
using Elsa.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Samples.WatchDirectoryWorker.Workflows
{
    public class WatchDirectoryCreatedWorkflow : IWorkflow
    {
        private readonly string _systemRoot = Path.GetPathRoot(Environment.SystemDirectory);

        public void Build(IWorkflowBuilder builder) => builder.WatchDirectory(setup => setup.WithPath(Path.Combine(_systemRoot,"Temp\\FileWatchers"))
                .WithPattern("*.txt")
                .WithChangeTypes(WatcherChangeTypes.Created))
            .WriteLine(setup => setup.WithText(context => $"{GetType().Name}-{context.GetInput<FileSystemEvent>().FullPath}-{context.GetInput<FileSystemEvent>().ChangeType}"));
    }
}
