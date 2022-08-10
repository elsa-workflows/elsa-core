using System;

namespace Elsa.Jobs.Services;

public class JobFactory : IJobFactory
{
    public IJob Create(Type jobType)
    {
        throw new NotImplementedException();
    }
}