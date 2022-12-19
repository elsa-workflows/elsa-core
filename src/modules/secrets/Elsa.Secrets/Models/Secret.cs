using System.Collections.Generic;
using System.Linq;
using Elsa.Expressions;
using Elsa.Models;

namespace Elsa.Secrets.Models
{
    public class Secret : Entity
    {
        public string Type { get; set; } = default!;
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public ICollection<SecretProperty> Properties { get; set; }

        public string GetProperty(string name, string syntax = SyntaxNames.Literal) => Properties?.FirstOrDefault(r => r.Name == name)?.GetExpression(syntax);
    }
}
