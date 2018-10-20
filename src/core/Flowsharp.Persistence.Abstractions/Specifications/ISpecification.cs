namespace Flowsharp.Persistence.Specifications
{
    public interface ISpecification<in T>
    {
        bool IsSatisfiedBy(T item);
    }
    
    public interface ISpecification<in T, in TVisitor> : ISpecification<T> where TVisitor : ISpecificationVisitor<TVisitor, T>
    {
        void Accept(TVisitor visitor);
    }
}
