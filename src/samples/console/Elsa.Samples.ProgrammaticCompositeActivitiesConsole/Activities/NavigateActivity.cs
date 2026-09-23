using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Attributes;
using Elsa.Builders;
using Elsa.Services;
using Elsa.Services.Models;
using System.Text;

namespace Elsa.Samples.ProgrammaticCompositeActivitiesConsole.Activities
{
    /// <summary>
    /// Custom activities that inherit from <seealso cref="CompositeActivity"/> declare their own mini-workflow.
    /// </summary>
    [Action(Outcomes = new[] { "Left", "Right" })]
    public class NavigateActivity : CompositeActivity
    {
        public override void Build(ICompositeActivityBuilder builder)
        {
            builder
                .StartWith(GetInstructions)
                .WriteLine(context => (string)context.GetInput<ActivityOutput>()!.Value)
                .ReadLine()
                .Finish(context => Capitalise(context.GetInput<string>()));
        }

        private static void GetInstructions(ActivityExecutionContext context) => context.Output = new ActivityOutput("Turn left or right?");

        // Taken from unmiantained package NetBox (@aloneguid)
        private static string Capitalise(string s)
        {
            if (s == null)
            {
                return null!;
            }

            StringBuilder stringBuilder = new();
            for (int i = 0; i < s.Length; i++)
            {
                stringBuilder.Append((i == 0) ? char.ToUpper(s[i]) : char.ToLower(s[i]));
            }

            return stringBuilder.ToString();
        }
    }
}