using System;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IActivityBuilder
    {
        IActivity Activity { get; }
        IOutcomeBuilder When(string outcome);
        IActivityBuilder Then<T>(Action<T> setup = null) where T : IActivity;
        Workflow Build();
    }
}