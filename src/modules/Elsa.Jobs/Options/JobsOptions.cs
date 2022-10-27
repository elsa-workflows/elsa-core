namespace Elsa.Jobs.Options;

public class JobsOptions
{
    /// <summary>
    /// The maximum number of workers to create to process jobs.
    /// </summary>
    public int WorkerCount { get; set; } = 10;
}