using System.Threading.Channels;
using Elsa.Mediator.Models;

namespace Elsa.Mediator.Contracts;

/// <summary>
/// A channel that can be used to enqueue jobs.
/// </summary>
public interface IJobChannel
{
    /// <summary>
    /// Gets the writer for the job queue.
    /// </summary>
    ChannelWriter<EnqueuedJob> Writer { get; }
    
    /// <summary>
    /// Gets the reader for the job queue.
    /// </summary>
    ChannelReader<EnqueuedJob> Reader { get; }
}