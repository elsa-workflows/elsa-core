using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Options;
using Elsa.Activities.Conductor.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Conductor.Providers.Events
{
    public class OptionsEventsProvider : IEventsProvider
    {
        private readonly ConductorOptions _options;
        public OptionsEventsProvider(IOptions<ConductorOptions> options) => _options = options.Value;
        public ValueTask<IEnumerable<EventDefinition>> GetEventsAsync(CancellationToken cancellationToken) => new(_options.Events);
    }
}