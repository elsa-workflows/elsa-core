namespace Elsa.Metadata
{
    public class ActivityPropertyDescriptor
    {
        public ActivityPropertyDescriptor()
        {
        }

        public ActivityPropertyDescriptor(string name, string uiHint, string label, string? hint = default, object? options = default, string? category = default)
        {
            Name = name;
            UIHint = uiHint;
            Label = label;
            Hint = hint;
            Options = options;
            Category = category;
            DefaultValue = defaultValue;
        }
        
        public string Name { get; set; } = default!;
        public string UIHint { get; set; } = default!;
        public string Label { get; set; } = default!;
        public string? Hint { get; set; }
        public object? Options { get; set; }
        public string? Category { get; set; }
        public object? DefaultValue { get; set; }
    }
}