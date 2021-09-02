namespace Elsa.Models
{
    public class SimpleExceptionProperty
    {
        public SimpleExceptionProperty(string name, object value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }
        public object Value { get; set; }
    }
}