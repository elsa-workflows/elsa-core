using Elsa.ActivityResults;
using Elsa.Services;
using Elsa.Services.Models;

namespace Elsa.Samples.GoBackConsole.Activities
{
    /// <summary>
    /// A custom activity that instructs the workflow runner to go back one step under certain conditions.
    /// </summary>
    public class BrickWallActivity : Activity
    {
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            // Tell the workflow that it hit a brick wall.
            return new GoBackResult("Brick Wall");
        }
    }
}