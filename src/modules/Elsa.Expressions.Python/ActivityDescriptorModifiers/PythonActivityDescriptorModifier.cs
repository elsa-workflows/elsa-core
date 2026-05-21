using Elsa.Expressions.Python.Options;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Expressions.Python.ActivityDescriptorModifiers;

internal class PythonActivityDescriptorModifier(IOptions<PythonOptions> options) : IActivityDescriptorModifier
{
    public void Modify(ActivityDescriptor descriptor)
    {
        if (descriptor.TypeName != WorkflowScriptActivityTypeNames.RunPython)
            return;

        descriptor.IsBrowsable = options.Value.AllowHostCodeExecution;
    }
}
