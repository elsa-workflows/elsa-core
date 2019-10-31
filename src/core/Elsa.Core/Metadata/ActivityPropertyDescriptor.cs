namespace Elsa.Metadata
{
    public class ActivityPropertyDescriptor
    {
        public ActivityPropertyDescriptor(string name, string type, string label, string hint = null, object options = null)
        {
            Name = name;
            Type = type;
            Label = label;
            Hint = hint;
            Options = options;
        }
        
        public string Name { get; }
        public string Type { get; }
        public string Label { get; }
        public string Hint { get; }
        public object Options { get; }
    }
}