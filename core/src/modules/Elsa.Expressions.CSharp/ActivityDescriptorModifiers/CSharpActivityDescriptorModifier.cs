using Elsa.Expressions.CSharp.Options;
using Elsa.Workflows;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Expressions.CSharp.ActivityDescriptorModifiers;

internal class CSharpActivityDescriptorModifier(IOptions<CSharpOptions> options) : IActivityDescriptorModifier
{
    public void Modify(ActivityDescriptor descriptor)
    {
        if (descriptor.TypeName != WorkflowScriptActivityTypeNames.RunCSharp)
            return;

        descriptor.IsBrowsable = options.Value.AllowHostCodeExecution;
    }
}
