using Flowsharp.Persistence.Specifications;

namespace Flowsharp.Persistence.Primitives
{
    public class AndSpecification<T, TVisitor> : ISpecification<T, TVisitor> where TVisitor : ISpecificationVisitor<TVisitor, T>
    {
        public ISpecification<T, TVisitor> Left { get; }
        public ISpecification<T, TVisitor> Right { get; }

        public AndSpecification(ISpecification<T, TVisitor> left, ISpecification<T, TVisitor> right)
        {
            Left = left;
            Right = right;
        }

        public void Accept(TVisitor visitor) => visitor.Visit(this);
        public bool IsSatisfiedBy(T obj) => Left.IsSatisfiedBy(obj) && Right.IsSatisfiedBy(obj);
    }
}
