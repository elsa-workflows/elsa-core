using Elsa.Jobs.Models;

namespace Elsa.Jobs.Contracts;

/// <summary>
/// Implemented by types that represent a background job.
/// </summary>
public interface IJob
{
    string Id { get; set; }
    ValueTask ExecuteAsync(JobExecutionContext context);
}