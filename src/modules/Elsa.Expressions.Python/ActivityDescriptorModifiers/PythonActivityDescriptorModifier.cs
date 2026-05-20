using Elsa.Expressions.Python.Options;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Expressions.Python.ActivityDescriptorModifiers;

internal class PythonActivityDescriptorModifier(IOptions<PythonOptions> options) : IActivityDescriptorModifier
{
    private static readonly string RunPythonActivityType = ActivityTypeNameHelper.GenerateTypeName<Activities.RunPython>();

    public void Modify(ActivityDescriptor descriptor)
    {
        if (descriptor.TypeName != RunPythonActivityType)
            return;

        descriptor.IsBrowsable = options.Value.AllowHostCodeExecution;
    }
}
