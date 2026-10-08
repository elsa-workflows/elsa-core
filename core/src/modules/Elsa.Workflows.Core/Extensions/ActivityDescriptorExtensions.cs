using Elsa.Workflows;
using Elsa.Workflows.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions for <see cref="ActivityDescriptor"/>.
/// </summary>
public static class ActivityDescriptorExtensions
{
    extension(ActivityDescriptor activityDescriptor)
    {
        /// <summary>
        /// Returns the specified attribute from the activity descriptor.
        /// </summary>
        public T? GetAttribute<T>() where T : Attribute
        {
            return (T?)activityDescriptor.Attributes.FirstOrDefault(x => x.GetType() == typeof(T));
        }

        /// <summary>
        /// Returns each wrapped input from the specified activity.
        /// </summary>
        public IEnumerable<InputDescriptor> GetWrappedInputPropertyDescriptors(IActivity activity)
        {
            return activityDescriptor.Inputs.Where(x => x.IsWrapped);
        }

        /// <summary>
        /// Returns each naked input from the specified activity.
        /// </summary>
        public IEnumerable<InputDescriptor> GetNakedInputPropertyDescriptors(IActivity activity)
        {
            return activityDescriptor.Inputs.Where(x => !x.IsWrapped);
        }

        /// <summary>
        /// Returns each input from the specified activity.
        /// </summary>
        public IDictionary<string, Input?> GetWrappedInputProperties(IActivity activity)
        {
            var wrappedInputDescriptors = activityDescriptor.Inputs.Where(x => x.IsWrapped).ToList();
            var inputLookup = wrappedInputDescriptors.ToDictionary(x => x.Name, x => (Input?)x.ValueGetter(activity));
            return inputLookup;
        }

        /// <summary>
        /// Returns each input descriptor from the specified activity.
        /// </summary>
        public InputDescriptor? GetWrappedInputPropertyDescriptor(IActivity activity, string name)
        {
            return activityDescriptor.Inputs.FirstOrDefault(x => x.Name == name && x.IsWrapped);
        }

        /// <summary>
        /// Returns each input from the specified activity.
        /// </summary>
        public Input? GetWrappedInputProperty(IActivity activity, string name)
        {
            var inputDescriptor = activityDescriptor.Inputs.FirstOrDefault(x => x.Name == name && x.IsWrapped);
            return inputDescriptor?.ValueGetter(activity) as Input;
        }
    }
}