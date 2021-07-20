using Elsa.Activities.Console;
using Elsa.Activities.File;
using Elsa.Builders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Samples.FileWatcher
{
    public class FileWatcherWorkflowIgnore : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder.Add<WatchDirectory>(setup =>
            {
                setup.Set(x => x.Directory, "C:\\Temp\\FileWatcher2");
                setup.Set(x => x.ChangeType, WatcherChangeTypes.Created);
            })
            .WriteLine("FileWatcherWorkflowSkip received file");
        }
    }
}
