using Elsa.Workflows.Core.Models;

namespace Elsa.Activities.Jobs.Activities;

/// <summary>
/// Executes a given job, suspending execution of the workflow until the job finishes.
/// </summary>
public class JobActivity : ActivityBase
{
    public JobActivity()
    {
    }

    public JobActivity(Type jobType)
    {
        JobType = jobType;
    }
    
    public Type JobType { get; set; } = default!;
}