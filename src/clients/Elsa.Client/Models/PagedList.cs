namespace Elsa.Client.Models
{
    public class PagedList<T> : List<T>
    {
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public int TotalCount { get; set; }
    }
}