using Elsa.Runtime.Contracts;

namespace Elsa.Runtime.Options;

public class WorkflowRuntimeOptions
{
    /// <summary>
    /// A list of workflow builders configured at application startup.
    /// </summary>
    public IDictionary<string, IWorkflow> Workflows { get; set; } = new Dictionary<string, IWorkflow>();
}