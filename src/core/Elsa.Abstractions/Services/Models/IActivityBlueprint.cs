using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Elsa.Services.Models
{
    public interface IActivityBlueprint
    {
        public string Id { get; }
        public string Type { get; }
        public JObject Data { get; }
        Func<ActivityExecutionContext, CancellationToken, ValueTask<IActivity>> CreateActivityAsync { get; }
    }
}