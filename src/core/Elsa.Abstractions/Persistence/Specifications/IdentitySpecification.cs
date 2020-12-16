using System;
using System.Linq.Expressions;

namespace Elsa.Persistence.Specifications
{
    internal struct IdentitySpecification<T> : ISpecification<T>
    {
        public bool IsSatisfiedBy(T entity) => true;
        public Expression<Func<T, bool>> ToExpression() => x => true;
    }
}
