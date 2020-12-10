namespace Elsa.Persistence.DocumentDb
{
    public class DocumentDbCollectionInfo
    {
        public string Name { get; set; }
        public string TenantId { get; set; }
        public int OfferThroughput { get; set; }
    }
}