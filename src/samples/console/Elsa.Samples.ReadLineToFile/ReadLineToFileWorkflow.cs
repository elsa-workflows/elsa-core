using Elsa.Activities.Console;
using Elsa.Activities.File;
using Elsa.Activities.Primitives;
using Elsa.Builders;
using System;
using System.Collections.Generic;
using System.IO;
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
                    setup.WithContent(context => context.GetOutputFrom<string>("ReadLine"));
                    setup.WithPath(context => context.GetOutputFrom<string>("TempFile"));
                    setup.WithMode(CopyMode.Overwrite);
                })
                .DeleteFile(setup => setup.WithPath(context => context.GetOutputFrom<string>("TempFile")));
        }
    }
}
