using System;
using System.Linq;
using System.Linq.Expressions;

namespace Elsa.Persistence.Specifications
{
    public struct NotSpecification<T> : ISpecification<T>
    {
        private readonly ISpecification<T> _specification;

        public NotSpecification(ISpecification<T> specification)
        {
            _specification = specification;
        }

        public bool IsSatisfiedBy(T entity)
        {
            return !_specification.IsSatisfiedBy(entity);
        }

        public Expression<Func<T, bool>> ToExpression()
        {
            var expression = _specification.ToExpression();
            return Expression.Lambda<Func<T, bool>>(
                Expression.Not(expression.Body),
                expression.Parameters.Single()
            );
        }
    }
}
