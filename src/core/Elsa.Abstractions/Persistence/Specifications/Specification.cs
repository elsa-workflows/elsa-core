using System;
using System.Linq.Expressions;

namespace Elsa.Persistence.Specifications
{
    /// <summary>
    /// This base specification implements the IsSatisfiedBy method by compiling the expression from ToExpression.
    ///
    /// This is useful for general specifications to prevent duplicated logic.
    /// Beware that it is not very performant in situations where many specifications are dynamically constructed and combined.   
    /// </summary>
    public abstract class Specification<T> : ISpecification<T>
    {
        public static readonly ISpecification<T> Identity = new IdentitySpecification<T>();
        public static readonly ISpecification<T> None = Identity.Not();
        private Func<T, bool>? _predicate;

        public virtual bool IsSatisfiedBy(T entity)
        {
            _predicate ??= ToExpression().Compile();
            return _predicate(entity);
        }

        public abstract Expression<Func<T, bool>> ToExpression();
    }
}