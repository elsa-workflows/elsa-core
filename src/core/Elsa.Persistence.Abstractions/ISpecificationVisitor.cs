using Elsa.Persistence.Specifications.Primitives;

namespace Elsa.Persistence
{
    public interface ISpecificationVisitor<TVisitor, T> where TVisitor : ISpecificationVisitor<TVisitor, T>
    {
        void Visit(And<T, TVisitor> specification);
        void Visit(Or<T, TVisitor> specification);
        void Visit(Not<T, TVisitor> specification);
        void Visit(All<T, TVisitor> specification);
    }
}