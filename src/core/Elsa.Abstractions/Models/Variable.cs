using System;

namespace Elsa.Models
{
    public class Variable
    {
        public static Variable? From(object? value)
        {
            return value != null ? new Variable(value) : null;
        }
        
        public Variable()
        {
        }

        public Variable(object value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }
        
        public object? Value { get; set; }
    }
}