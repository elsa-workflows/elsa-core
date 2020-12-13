using System;
using System.Linq.Expressions;

namespace Elsa.Specifications
{
    internal struct IdentitySpecification<T> : ISpecification<T>
    {
        public bool IsSatisfiedBy(T entity) => true;
        public Expression<Func<T, bool>> ToExpression() => x => true;
    }
}
