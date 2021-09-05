using Elsa.Activities.Console;
using Elsa.Activities.File;
using Elsa.Activities.File.Models;
using Elsa.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Samples.WatchDirectoryWorker.Workflows
{
    public class WatchDirectoryWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder) => builder.WatchDirectory(setup => setup.WithPath("C:\\Temp")
                                                         .WithPattern("*.txt"))
            .WriteLine(setup => setup.WithText(async context => context.GetInput<FileSystemEvent>().FullPath));
    }
}
