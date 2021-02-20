namespace Elsa.Client.Models
{
    public class ActivityDefinitionProperty
    {
        public static ActivityDefinitionProperty Literal(string name, string expression) => new ActivityDefinitionProperty(name, expression, "Literal");
        public static ActivityDefinitionProperty Liquid(string name, string expression) => new ActivityDefinitionProperty(name, expression, "Liquid");
        public static ActivityDefinitionProperty JavaScript(string name, string expression) => new ActivityDefinitionProperty(name, expression, "JavaScript");

        public ActivityDefinitionProperty()
        {
        }

        public ActivityDefinitionProperty(string name, string expression, string syntax)
        {
            Name = name;
            Expression = expression;
            Syntax = syntax;
        }

        public string Name { get; set; } = default!;
        public string Syntax { get; set; } = default!;
        public string Expression { get; set; } = default!;
    }
}