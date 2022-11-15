using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Runtime.Options;

public class WorkflowRuntimeOptions
{
    /// <summary>
    /// A list of workflow builders configured during application startup.
    /// </summary>
    public IDictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>> Workflows { get; set; } = new Dictionary<string, Func<IServiceProvider, ValueTask<IWorkflow>>>();
}