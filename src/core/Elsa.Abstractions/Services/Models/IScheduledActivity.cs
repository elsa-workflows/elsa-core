using Elsa.Models;

namespace Elsa.Services.Models
{
    public interface IScheduledActivity
    {
        ActivityDefinition ActivityDefinition { get; }
        object? Input { get; }
    }
}