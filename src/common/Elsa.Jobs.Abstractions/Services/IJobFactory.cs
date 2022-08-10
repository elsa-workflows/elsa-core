using System;

namespace Elsa.Jobs.Services;

/// <summary>
/// Instantiates new jobs of a given type.
/// </summary>
public interface IJobFactory
{
    IJob Create(Type jobType);
}