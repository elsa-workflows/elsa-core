using System;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using HotChocolate.Language;
using HotChocolate.Types;

namespace Elsa.Server.GraphQL.Types
{
    public class VariableType : ScalarType
    {
        private readonly IWorkflowSerializer serializer;

        public VariableType(IWorkflowSerializer serializer) : base("Variable")
        {
            this.serializer = serializer;
        }
        
        public override Type ClrType => typeof(Variable);

        public override bool IsInstanceOfType(IValueNode literal) => literal == null || literal is StringValueNode || literal is NullValueNode;

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null || literal is NullValueNode)
                return new Variable();

            if (literal is StringValueNode stringNode)
            {
                var variable = serializer.Deserialize<Variables>(stringNode.Value);
                return variable;
            }
            
            throw new ArgumentException("The Variable type can only parse string literals.", nameof(literal));
        }

        public override IValueNode ParseValue(object value)
        {
            var stringValue = serializer.Serialize(value, JsonTokenFormatter.FormatName);
            return new StringValueNode(stringValue);
        }

        public override object Serialize(object value)
        {
            if (value == null)
                return null;

            return serializer.Serialize(value, JsonTokenFormatter.FormatName);
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is string s)
            {
                value = serializer.Deserialize<Variable>(s);
                return true;
            }

            value = null;
            return false;
        }

        
    }
}