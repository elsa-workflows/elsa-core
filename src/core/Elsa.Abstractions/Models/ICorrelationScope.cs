namespace Elsa.Models
{
    public interface ICorrelationScope
    {
        string? CorrelationId { get; }
    }
}