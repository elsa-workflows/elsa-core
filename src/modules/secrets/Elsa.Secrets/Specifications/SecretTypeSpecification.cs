using Elsa.Persistence.Specifications;
using Elsa.Secrets.Models;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Elsa.Secrets.Specifications
{
    public class SecretTypeSpecification : Specification<Secret>
    {
        public string? Type { get; set; }
        public SecretTypeSpecification(string? type) => Type = type;
        public override Expression<Func<Secret, bool>> ToExpression() => x => x.Type == Type;
    }
}
