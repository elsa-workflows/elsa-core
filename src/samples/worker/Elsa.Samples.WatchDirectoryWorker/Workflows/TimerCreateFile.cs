using Elsa.Activities.File;
using Elsa.Activities.Temporal;
using Elsa.Builders;
using NodaTime;
using System;
using System.IO;
using System.Text;

namespace Elsa.Samples.WatchDirectoryWorker.Workflows
{
    public class TimerCreateFile : IWorkflow
    {
        private IClock _clock;
        private string _directory;

        public TimerCreateFile(IClock clock, string directory)
        {
            _clock = clock;
            _directory = directory;
        }

        public void Build(IWorkflowBuilder builder) => builder
            .Timer(Duration.FromSeconds(10))
            .OutFile(setup =>
            {
                var guid = Guid.NewGuid().ToString();
                var filename = Path.Combine(_directory, $"{guid}.txt");

                setup.WithPath(filename);
                setup.WithBytes(Encoding.UTF8.GetBytes("Hello World"));
            });
    }
}
