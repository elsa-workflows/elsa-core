using Elsa.Extensions;
using Elsa.Workflows.Helpers;
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
            var activityDescriptor = context.ActivityDescriptor;
            var outputDescriptors = activityDescriptor.Outputs;

            var outputs = outputDescriptors.ToDictionary(x => x.Name, x =>
            {
                if (x.IsSerializable == false)
                    return "(not serializable)";

                // Expand a dictionary of outputs (Dictionary<string, Output<T>>) into its individual named values.
                // Each entry is read like a single output, from the register under its composite name first.
                if (x.IsDictionaryWithValueOfOutput)
                {
                    var entries = InputOutputCollections.ReadDictionary<Output>(x.ValueGetter(activity));

                    if (entries == null)
                    {
                        return null!;
                    }

                    var expanded = new Dictionary<string, object?>();

                    foreach (var (key, output) in entries)
                    {
                        expanded[key] = ReadOutputValue(context, DictionaryEntryNames.GetOutputName(x.Name, key), output);
                    }

                    return expanded;
                }

                return ReadOutputValue(context, x.Name, x.ValueGetter(activity) as Output)!;
            });

            return outputs;
        }
    }

    private static object? ReadOutputValue(ActivityExecutionContext context, string outputName, Output? output)
    {
        // An output this execution recorded wins, even when the recorded value is null.
        var outputRegister = context.WorkflowExecutionContext.GetActivityOutputRegister();

        if (outputRegister.TryFindOutputByActivityInstanceId(context.Id, outputName, out var recordedValue))
        {
            return recordedValue;
        }

        if (output != null && context.TryGet(output.MemoryBlockReference(), out var outputValue))
        {
            return outputValue;
        }

        return null;
    }
}