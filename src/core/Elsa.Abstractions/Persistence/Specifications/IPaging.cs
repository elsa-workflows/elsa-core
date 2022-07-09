namespace Elsa.Persistence.Specifications
{
    public interface IPaging
    {
        int Skip { get; }
        int Take { get; }
    }

    public class Paging : IPaging
    {
        public static Paging Page(int page, int pageSize) => new(page * pageSize, pageSize);
        
        public Paging(int skip, int take)
        {
            Skip = skip;
            Take = take;
        }

        public int Skip { get; }
        public int Take { get; }

        public static Paging? Create(int? skip, int? take) => skip != null && take != null ? new Paging(skip.Value, take.Value) : default;
    }
}
