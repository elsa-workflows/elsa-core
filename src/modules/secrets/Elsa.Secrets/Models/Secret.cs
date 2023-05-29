using System.Collections.Generic;
using System.Linq;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Secrets.Models
{
    public class Secret : Entity
    {
        public string Type { get; set; } = default!;
        public string Name { get; set; } = null!;
        public string? DisplayName { get; set; }
        public ICollection<SecretProperty> Properties { get; set; }

        public string? GetProperty(string name, string syntax = SyntaxNames.Literal) => Properties?.FirstOrDefault(r => r.Name == name)?.GetExpression(syntax);

        public void AddOrUpdateProperty(string name, string value, string syntax = SyntaxNames.Literal)
        {
            RemoveProperty(name);
            var property = new SecretProperty(name, new Dictionary<string, string?> { [syntax] = value }, syntax);
            Properties.Add(property);
        }
        
        public void RemoveProperty(string name)
        {
            var property = Properties.FirstOrDefault(x => x.Name == name);
            if (property != null)
            {
                Properties.Remove(property);
            }
        }
    }
}
