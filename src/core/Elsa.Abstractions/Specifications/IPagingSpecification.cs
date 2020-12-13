namespace Elsa.Specifications
{
    public interface IPagingSpecification
    {
        int Skip { get; }
        int Take { get; }
    }

    public class PagingSpecification : IPagingSpecification
    {
        public static PagingSpecification Page(int skip, int take) => new(skip, take);
        
        public PagingSpecification(int skip, int take)
        {
            Skip = skip;
            Take = take;
        }

        public int Skip { get; }
        public int Take { get; }
    }
}
