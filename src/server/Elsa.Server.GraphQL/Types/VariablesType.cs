using System;
using Elsa.Models;
using Elsa.Serialization;
using Elsa.Serialization.Formatters;
using HotChocolate;
using HotChocolate.Language;
using HotChocolate.Types;
using Newtonsoft.Json;

namespace Elsa.Server.GraphQL.Types
{
    public class VariablesType : ScalarType
    {
        private readonly IWorkflowSerializer serializer;

        public VariablesType(IWorkflowSerializer serializer) : base("Variables")
        {
            this.serializer = serializer;
        }

        public override Type ClrType => typeof(Variables);

        public override bool IsInstanceOfType(IValueNode literal) => literal == null || literal is StringValueNode || literal is NullValueNode;

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null || literal is NullValueNode)
                return new Variables();

            if (literal is StringValueNode stringNode)
            {
                var variables = serializer.Deserialize<Variables>(stringNode.Value, JsonTokenFormatter.FormatName);
                return variables;
            }

            throw new ArgumentException("The Variable type can only parse string literals.", nameof(literal));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value is null)
                return NullValueNode.Default;

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
                value = serializer.Deserialize<Variables>(s, JsonTokenFormatter.FormatName);
                return true;
            }

            value = null;
            return false;
        }
    }
}