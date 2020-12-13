using System;
using System.Linq.Expressions;

namespace Elsa.Specifications
{
    public interface ISpecification<T>
    {
        bool IsSatisfiedBy(T entity);
        Expression<Func<T, bool>> ToExpression();
    }
}
