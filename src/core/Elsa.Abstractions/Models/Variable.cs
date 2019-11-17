using System;
using Newtonsoft.Json.Linq;

namespace Elsa.Models
{
    public class Variable
    {
        public Variable()
        {
        }

        public Variable(object value)
        {
            if (value != null)
            {
                TypeName = value.GetType().AssemblyQualifiedName;
                Token = JToken.FromObject(value);
            }
        }
        
        public string TypeName { get; set; }
        public JToken Token { get; set; }

        public object GetValue()
        {
            if (Token == null)
                return null;
            
            var type = Type.GetType(TypeName);
            return Token.ToObject(type);
        }
    }
}