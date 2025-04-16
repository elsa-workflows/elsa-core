using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

public static partial class ActivityExecutionContextExtensions
{
    public static IDictionary<string, object?> GetInputs(this ActivityExecutionContext context)
    {
        return context.ActivityState!;
    }
    
    public static IDictionary<string, object?> GetOutputs(this ActivityExecutionContext context)
    {
        var activity = context.Activity;
        var expressionExecutionContext = context.ExpressionExecutionContext;
        var activityDescriptor = context.ActivityDescriptor;
        var outputDescriptors = activityDescriptor.Outputs;

        var outputs = outputDescriptors.ToDictionary(x => x.Name, x =>
        {
            if (x.IsSerializable == false)
                return "(not serializable)";

            var cachedValue = activity.GetOutput(expressionExecutionContext, x.Name);

            if (cachedValue != null)
                return cachedValue;

            if (x.ValueGetter(activity) is Output output && context.TryGet(output.MemoryBlockReference(), out var outputValue))
                return outputValue;

            return null;
        });

        return outputs;
    }
}