namespace Sample26.Models
{
    public class Order
    {
        public string Id { get; set; }
        public Customer Customer { get; set; }
        public string Product { get; set; }
        public decimal Amount { get; set; }
    }
}