using Elsa.Expressions.CSharp.Options;
using Elsa.Workflows;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Expressions.CSharp.ActivityDescriptorModifiers;

internal class CSharpActivityDescriptorModifier(IOptions<CSharpOptions> options) : IActivityDescriptorModifier
{
    private static readonly string RunCSharpActivityType = ActivityTypeNameHelper.GenerateTypeName<Activities.RunCSharp>();

    public void Modify(ActivityDescriptor descriptor)
    {
        if (descriptor.TypeName != RunCSharpActivityType)
            return;

        descriptor.IsBrowsable = options.Value.AllowHostCodeExecution;
    }
}
