using Flowsharp.Persistence.Primitives;

namespace Flowsharp.Persistence.Specifications
{
    public interface ISpecificationVisitor<TVisitor, T> where TVisitor : ISpecificationVisitor<TVisitor, T>
    {
        void Visit(AndSpecification<T, TVisitor> specification);
        void Visit(OrSpecification<T, TVisitor> specification);
        void Visit(NotSpecification<T, TVisitor> specification);
    }
}