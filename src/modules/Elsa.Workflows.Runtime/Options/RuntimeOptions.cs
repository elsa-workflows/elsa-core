using Elsa.Workflows.Core.Contracts;

namespace Elsa.Workflows.Runtime.Options;

/// <summary>
/// Provides options to the workflow runtime.
/// </summary>
public class RuntimeOptions
{
    /// <summary>
    /// A list of workflow builders configured during application startup.
    /// </summary>
    public IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> Workflows { get; set; } = new Dictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>>();
}