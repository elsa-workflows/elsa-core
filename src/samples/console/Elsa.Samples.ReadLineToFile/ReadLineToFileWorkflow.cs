using Elsa.Activities.Console;
using Elsa.Activities.File;
using Elsa.Builders;
using System.Text;

namespace Elsa.Samples.ReadLineToFile
{
    public class ReadLineToFileWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("Enter text to write to file")
                .ReadLine()
                .WithName("ReadLine")
                .TempFile()
                .WithName("TempFile")
                .OutFile(setup =>
                {
                    setup.WithBytes(async context => Encoding.UTF8.GetBytes(await context.GetNamedActivityPropertyAsync<ReadLine, string>("ReadLine", x => x.Output)));
                    setup.WithPath(async context => await context.GetNamedActivityPropertyAsync<TempFile, string>("TempFile", x => x.Path));
                    setup.WithMode(CopyMode.Overwrite);
                })
                .WriteLine(setup => setup.WithText(async context => await context.GetNamedActivityPropertyAsync<TempFile, string>("TempFile", x => x.Path)))
                .WriteLine("Press any key to continue")
                .ReadLine()
                .DeleteFile(setup => setup.WithPath(async context => await context.GetNamedActivityPropertyAsync<TempFile, string>("TempFile", x => x.Path)));
        }
    }
}
