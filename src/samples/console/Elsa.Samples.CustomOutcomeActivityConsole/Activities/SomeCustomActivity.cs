using System;
using Elsa.ActivityResults;
using Elsa.Services;

namespace Elsa.Samples.CustomOutcomeActivityConsole.Activities
{
    // The important aspect of this activity is that it returns an outcome other than "Done".
    // Despite that, we still want to be able to execute any activities connected to this one if that connection is established with the "Done" outcome, because that's what the Workflow Builder API uses when connecting activities implicitly.
    public class SomeCustomActivity : Activity
    {
        protected override IActivityExecutionResult OnExecute()
        {
            Console.WriteLine("Executing custom activity.");
            return Outcome("Next");
        }
    }
}