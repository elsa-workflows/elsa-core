using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Options;
using Elsa.Activities.Conductor.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Conductor.Providers.Tasks
{
    public class OptionsTasksProvider : ITasksProvider
    {
        private readonly ConductorOptions _options;
        public OptionsTasksProvider(IOptions<ConductorOptions> options) => _options = options.Value;
        public ValueTask<IEnumerable<TaskDefinition>> GetTasksAsync(CancellationToken cancellationToken) => new(_options.Tasks);
    }
}