using System;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IOutcomeBuilder
    {
        IActivityBuilder Source { get; }
        string Outcome { get; }
        IActivityBuilder Then<T>(Action<T> setup) where T : IActivity;
        Workflow Build();
    }
}