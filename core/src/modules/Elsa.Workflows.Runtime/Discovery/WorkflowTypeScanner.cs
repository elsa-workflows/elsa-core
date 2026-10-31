using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Elsa.Workflows.Runtime.Discovery;

internal static class WorkflowTypeScanner
{
    [RequiresUnreferencedCode("The assembly is required to be referenced.")]
    public static IEnumerable<Type> GetWorkflowTypes(Assembly assembly)
    {
        return assembly.GetExportedTypes()
            .Where(x => typeof(IWorkflow).IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false });
    }
}
