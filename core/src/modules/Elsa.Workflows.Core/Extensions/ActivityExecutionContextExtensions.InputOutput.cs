using System.Collections;
using Elsa.Extensions;
using Elsa.Workflows.Models;

namespace Elsa.Workflows;

public static class ActivityExecutionContextExtensions
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

                // Expand a dictionary of outputs (Dictionary<string, Output<T>>) into its individual named values.
                if (x.IsDictionaryWithValueOfOutput && x.ValueGetter(activity) is IDictionary dictionary)
                {
                    var expanded = new Dictionary<string, object?>();

                    foreach (DictionaryEntry entry in dictionary)
                    {
                        var key = entry.Key.ToString()!;
                        object? value = null;

                        if (entry.Value is Output output && context.TryGet(output.MemoryBlockReference(), out var outputValue))
                            value = outputValue;

                        expanded[key] = value;
                    }

                    return expanded;
                }

                var cachedValue = activity.GetOutput(expressionExecutionContext, x.Name);

                if (cachedValue != null)
                    return cachedValue;

                if (x.ValueGetter(activity) is Output singleOutput && context.TryGet(singleOutput.MemoryBlockReference(), out var singleOutputValue))
                    return singleOutputValue!;

                return null!;
            });

            return outputs;
        }
    }
}