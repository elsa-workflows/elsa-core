using System;
using System.Linq.Expressions;
using Elsa.Models;

namespace Elsa.Persistence.Specifications
{
    public class EntityIdSpecification<T> : Specification<T> where T:IEntity
    {
        public string? Id { get; set; }
        public EntityIdSpecification(string? id) => Id = id;
        public override Expression<Func<T, bool>> ToExpression() => x => x.Id == Id;
    }
}