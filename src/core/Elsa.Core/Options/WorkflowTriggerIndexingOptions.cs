using System;
using System.Collections.Generic;
using Elsa.Providers.Workflows;

namespace Elsa.Options;

public class WorkflowTriggerIndexingOptions
{
    /// <summary>
    /// A list of <see cref="IWorkflowProvider"/> types whose workflows should not be indexed during application startup.
    /// This is desirable for providers that potentially provide a large number of workflows, such as <see cref="DatabaseWorkflowProvider"/>. 
    /// </summary>
    public HashSet<Type> ExcludedProviders { get; } = new();
}