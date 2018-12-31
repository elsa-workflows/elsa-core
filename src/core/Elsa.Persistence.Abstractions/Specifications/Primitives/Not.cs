namespace Elsa.Persistence.Specifications.Primitives
{
    public class Not<T, TVisitor> : ISpecification<T, TVisitor> where TVisitor : ISpecificationVisitor<TVisitor, T>
    {
        public ISpecification<T, TVisitor> Specification { get; }

        public Not(ISpecification<T, TVisitor> specification)
        {
            Specification = specification;
        }

        public void Accept(TVisitor visitor) => visitor.Visit(this);
        public bool IsSatisfiedBy(T obj) => !Specification.IsSatisfiedBy(obj);
    }
}
