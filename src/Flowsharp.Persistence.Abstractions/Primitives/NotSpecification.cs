using Flowsharp.Persistence.Specifications;

namespace Flowsharp.Persistence.Primitives
{
    public class NotSpecification<T, TVisitor> : ISpecification<T, TVisitor> where TVisitor : ISpecificationVisitor<TVisitor, T>
    {
        public ISpecification<T, TVisitor> Specification { get; }

        public NotSpecification(ISpecification<T, TVisitor> specification)
        {
            Specification = specification;
        }

        public void Accept(TVisitor visitor) => visitor.Visit(this);
        public bool IsSatisfiedBy(T obj) => !Specification.IsSatisfiedBy(obj);
    }
}
