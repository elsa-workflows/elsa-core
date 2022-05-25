using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Samples.Console1.Workflows;

public static class ConditionalWorkflow
{
    public static IActivity Create()
    {
        var age = new Variable<int>();
            
        return new Sequence
        {
            Variables = { age },
            Activities =
            {
                new WriteLine("What's your age?"),
                new ReadLine(age),
                new If
                {
                    Condition = new Input<bool>(context => age.Get(context) >= 16),
                    Then = new Sequence(
                        new WriteLine("Enjoy your driver's license!"),
                        new WriteLine("But be careful!")),
                    Else = new WriteLine("Enjoy your bicycle!")
                }
            }
        };
    }
}