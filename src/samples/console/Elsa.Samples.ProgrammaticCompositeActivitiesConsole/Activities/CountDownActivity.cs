using Elsa.Activities.Console;
using Elsa.Builders;
using Elsa.Services;

namespace Elsa.Samples.ProgrammaticCompositeActivitiesConsole.Activities
{
    /// <summary>
    /// Custom activities that inherit from <seealso cref="CompositeActivity"/> declare their own mini-workflow.
    /// </summary>
    public class CountDownActivity : CompositeActivity
    {
        public override void Build(ICompositeActivityBuilder composite)
        {
            composite
                .WriteLine("3!")
                .WriteLine("2!")
                .WriteLine("1!");
        }
    }
}