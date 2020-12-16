using System;
using System.Linq.Expressions;
using LinqKit;

namespace Elsa.Persistence.Specifications
{
    internal struct AndSpecification<T> : ISpecification<T>
    {
        private readonly ISpecification<T> _left;
        private readonly ISpecification<T> _right;

        public AndSpecification(ISpecification<T> left, ISpecification<T> right)
        {
            _right = right;
            _left = left;
        }

        public bool IsSatisfiedBy(T entity) => _left.IsSatisfiedBy(entity) && _right.IsSatisfiedBy(entity);
        public Expression<Func<T, bool>> ToExpression() => _left.ToExpression().And(_right.ToExpression());
    }
}
