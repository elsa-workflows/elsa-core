using System;

namespace Elsa.Services.Models
{
    public interface IActivityBlueprint
    {
        public string Id { get; }
        Func<ActivityExecutionContext, IActivity> CreateActivity { get; }
    }
}