using System.Collections.Generic;

namespace Elsa.Secrets.Models
{
    public class SecretProperty
    {
        public static SecretProperty Literal(string name, string expression) => new(name, CreateSingleExpression("Literal", expression), null);
        public static SecretProperty Liquid(string name, string expression) => new(name, CreateSingleExpression("Liquid", expression), "Liquid");
        public static SecretProperty JavaScript(string name, string expression) => new(name, CreateSingleExpression("JavaScript", expression), "JavaScript");
        private static IDictionary<string, string?> CreateSingleExpression(string syntax, string expression) => new Dictionary<string, string?> { [syntax] = expression };

        public SecretProperty()
        {
        }

        public SecretProperty(string name, IDictionary<string, string?> expressions, string? syntax)
        {
            Name = name;
            Expressions = expressions;
            Syntax = syntax;
        }

        /// <summary>
        /// The name of the property.
        /// </summary>
        public string Name { get; set; } = default!;

        /// <summary>
        /// The configured syntax to use when selecting the expression to evaluate.
        /// </summary>
        /// <remarks>
        /// If no value is specified (i.e. null or empty), then an attempt will be made to select for the "Literal" syntax expression.
        /// </remarks>
        public string? Syntax { get; set; }

        /// <summary>
        /// Contains an expression for each supported syntax.
        /// </summary>
        public IDictionary<string, string?> Expressions { get; set; } = new Dictionary<string, string?>();

        public string? GetExpression(string syntax) => Expressions.ContainsKey(syntax) ? Expressions[syntax] : default;

        public override string ToString()
            => this.Name;
    }
}
