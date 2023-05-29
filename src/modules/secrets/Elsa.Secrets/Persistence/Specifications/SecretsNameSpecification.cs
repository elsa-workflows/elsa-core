using System;
using System.Linq.Expressions;
using Elsa.Persistence.Specifications;
using Elsa.Secrets.Models;

namespace Elsa.Secrets.Persistence.Specifications; 

public class SecretsNameSpecification : Specification<Secret>
{
    public string Name { get; set; }

    public SecretsNameSpecification(string name)
    {
        Name = name;
    }

    public override Expression<Func<Secret, bool>> ToExpression() => x => x.Name == Name;
}
