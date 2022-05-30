using System.Text;
using Elsa.Activities.Console;
using Elsa.Activities.File;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Elsa.Samples.ReadLineToFile
{
    public class ReadLineToFileWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("Enter text to write to file:")
                .ReadLine().WithName("ReadLine")
                .TempFile().WithName("TempFile")
                .SetVariable("TempFilePath", async context => await context.GetNamedActivityPropertyAsync<TempFile, string>("TempFile", x => x.Path))
                .OutFile(setup =>
                {
                    setup.WithBytes(async context => Encoding.UTF8.GetBytes((await context.GetNamedActivityPropertyAsync<ReadLine, string>("ReadLine", x => x.Output))!));
                    setup.WithPath(context => context.GetVariable<string>("TempFilePath"));
                    setup.WithMode(CopyMode.Overwrite);
                })
                
                .WriteLine(setup => setup.WithText(context => context.GetVariable<string>("TempFilePath")))
                .WriteLine("Press any key to read back the file")
                .ReadLine()
                .ReadFile(context => context.GetVariable<string>("TempFilePath")).WithName("ReadTempFile")
                .WriteLine(async context => Encoding.UTF8.GetString((await context.GetNamedActivityPropertyAsync<ReadFile, byte[]>("ReadTempFile", x => x.Bytes))!))
                .WriteLine("Press any key to delete the file")
                .ReadLine()
                .DeleteFile(setup => setup.WithPath(context => context.GetVariable<string>("TempFilePath")))
                .WriteLine("File deleted");
        }
    }
}