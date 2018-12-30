using Flowsharp.Persistence.Specifications.Primitives;

namespace Flowsharp.Persistence
{
    public interface ISpecificationVisitor<TVisitor, T> where TVisitor : ISpecificationVisitor<TVisitor, T>
    {
        void Visit(And<T, TVisitor> specification);
        void Visit(Or<T, TVisitor> specification);
        void Visit(Not<T, TVisitor> specification);
        void Visit(All<T, TVisitor> specification);
    }
}