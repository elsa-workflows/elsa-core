namespace Elsa.Services.Models
{
    public interface IScheduledActivity
    {
        string ActivityId { get; }
        object? Input { get; }
    }
}