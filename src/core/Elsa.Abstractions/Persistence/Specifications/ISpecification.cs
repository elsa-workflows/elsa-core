using System;
using System.Linq.Expressions;

namespace Elsa.Persistence.Specifications
{
    public interface ISpecification<T>
    {
        bool IsSatisfiedBy(T entity);
        Expression<Func<T, bool>> ToExpression();
    }
}
