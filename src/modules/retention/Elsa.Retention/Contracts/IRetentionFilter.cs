using System.Threading.Tasks;
using Elsa.Retention.Models;

namespace Elsa.Retention.Contracts;

/// <summary>
/// Represents a filter that returns a value indicating whether or not the provided workflow instance should be deleted or not.
/// There can be multiple implementations of this filter, where each filter is called in sequence until a filter is encountered that returns true.
/// </summary>
public interface IRetentionFilter
{
    ValueTask<bool> GetShouldDeleteAsync(RetentionFilterContext context);
}