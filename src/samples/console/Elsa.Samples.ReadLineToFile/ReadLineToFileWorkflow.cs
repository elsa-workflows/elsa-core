using Elsa.Activities.Console;
using Elsa.Activities.File;
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
                .OutFile(Path.GetTempFileName(), CopyMode.Overwrite);
        }
    }
}
