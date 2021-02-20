namespace Elsa.Client.Models
{
    public class ActivityScope
    {
        public string ActivityId { get; set; } = default!;
        public Variables Variables { get; set; } = new();
    }
}