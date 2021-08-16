using System;

namespace Elsa.Metadata
{
    public class ActivityOutputDescriptor
    {
        public ActivityOutputDescriptor()
        {
        }

        public ActivityOutputDescriptor(
            string name,
            Type type,
            string? hint = default,
            string? defaultWorkflowStorageProvider = default,
            bool disableWorkflowProviderSelection = false)
        {
            Name = name;
            Type = type;
            Hint = hint;
            DefaultWorkflowStorageProvider = defaultWorkflowStorageProvider;
            DisableWorkflowProviderSelection = disableWorkflowProviderSelection;
        }

        public string Name { get; set; } = default!;
        public Type Type { get; set; } = default!;
        public string? Hint { get; set; }
        public string? DefaultWorkflowStorageProvider { get; set; }
        public bool DisableWorkflowProviderSelection { get; set; }
    }
}