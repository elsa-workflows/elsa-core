using Elsa.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;


public static partial class ActivityExecutionContextExtensions
{
    extension(ActivityExecutionContext context)
    {
        public IDictionary<string, object> GetInputs()
        {
            return context.ActivityState!;
        }

        public IDictionary<string, object> GetOutputs()
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
                    return outputValue!;

                return null!;
            });

            return outputs;
        }
    }
}