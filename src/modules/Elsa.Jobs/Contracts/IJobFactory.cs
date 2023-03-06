namespace Elsa.Jobs.Contracts;

/// <summary>
/// Instantiates jobs of a given type.
/// </summary>
public interface IJobFactory
{
    /// <summary>
    /// Instantiates a job of the specified type.
    /// </summary>
    IJob Create(Type jobType);
}