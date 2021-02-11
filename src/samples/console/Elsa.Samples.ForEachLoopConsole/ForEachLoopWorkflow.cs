using System;
using System.Globalization;
using System.Linq;
using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Builders;

namespace Elsa.Samples.ForEachLoopConsole
{
    /// <summary>
    /// This workflow prompts the user to enter an integer start value, then iterates back from that value to 0.
    /// The workflow also demonstrates retrieving runtime values such as user input. 
    /// </summary>
    public class ForEachLoopWorkflow : IWorkflow
    {
        public void Build(IWorkflowBuilder builder)
        {
            builder
                .WriteLine("Enumerating all months of the year:")
                .ForEach(DateTimeFormatInfo.CurrentInfo!.MonthNames.Cast<object>().ToList, iterate => iterate.WriteLine(context => context.GetInput<string>()))
                .WriteLine("Done.");
        }
    }
}