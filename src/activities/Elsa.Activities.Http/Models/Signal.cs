namespace Elsa.Activities.Http.Models
{
    public class Signal
    {
        public Signal()
        {
        }

        public Signal(string name, string correlationId)
        {
            Name = name;
            CorrelationId = correlationId;
        }

        public string Name { get; set; } = default!;
        public string CorrelationId { get; set; } = default!;
    }
}