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
            string? hint = default)
        {
            Name = name;
            Type = type;
            Hint = hint;
        }

        public string Name { get; set; } = default!;
        public Type Type { get; set; } = default!;
        public string? Hint { get; set; }
    }
}