using System;
using Elsa.Services;

namespace Elsa.Builders
{
    public interface IActivityBuilder
    {
        IServiceProvider ServiceProvider { get; }
        IActivity Build();
    }
}