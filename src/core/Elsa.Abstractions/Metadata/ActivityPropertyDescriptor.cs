namespace Elsa.Metadata
{
    public class ActivityPropertyDescriptor
    {
        public ActivityPropertyDescriptor()
        {
        }

        public ActivityPropertyDescriptor(string name, string uiHint, string label, string? hint, object? options)
        {
            Name = name;
            UIHint = uiHint;
            Label = label;
            Hint = hint;
            Options = options;
        }
        
        public string Name { get; set; } = default!;
        public string UIHint { get; set; } = default!;
        public string Label { get; set; } = default!;
        public string? Hint { get; set; }
        public object? Options { get; set; }
    }
}