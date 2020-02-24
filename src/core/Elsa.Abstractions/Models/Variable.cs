namespace Elsa.Models
{
    public class Variable
    {
        public static Variable From(object value)
        {
            return value != null ? new Variable(value) : null;
        }
        
        public Variable()
        {
        }

        public Variable(object value)
        {
            Value = value;
        }
        
        public object? Value { get; set; }
    }
}