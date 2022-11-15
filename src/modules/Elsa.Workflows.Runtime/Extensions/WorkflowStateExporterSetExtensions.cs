using System;
using System.Collections.Generic;
using Elsa.Workflows.Runtime.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Runtime.Extensions;

public static class WorkflowStateExporterSetExtensions
{
    /// <summary>
    /// Register a workflow factory that creates an instance of the specified workflow type. 
    /// </summary>
    public static void Add<TExporter>(this ISet<Func<IServiceProvider, IWorkflowStateExporter>> set) where TExporter : IWorkflowStateExporter => set.Add(sp => ActivatorUtilities.GetServiceOrCreateInstance<TExporter>(sp));
}