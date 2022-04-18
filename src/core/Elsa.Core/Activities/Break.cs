using Elsa.Attributes;
using Elsa.Contracts;
using Elsa.Models;
using Microsoft.Extensions.Logging;

namespace Elsa.Activities;

[Activity("Elsa", "Control Flow", "Break out of a loop")]
public class Break : Activity
{
    protected override void Execute(ActivityExecutionContext context)
    {
        // Find the first parent looping construct.
        var loopingConstructContext = context.GetAncestorActivityExecutionContexts().FirstOrDefault(x => x.Activity is ILoopingConstruct);
        var loopingConstructActivity = (ILoopingConstruct?)loopingConstructContext?.Activity;
        
        if (loopingConstructActivity != null)
        {
            
        }
    }
}