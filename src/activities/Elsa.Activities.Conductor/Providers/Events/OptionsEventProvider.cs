using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Conductor.Models;
using Elsa.Activities.Conductor.Options;
using Elsa.Activities.Conductor.Services;
using Microsoft.Extensions.Options;

namespace Elsa.Activities.Conductor.Providers.Events
{
    public class OptionsEventProvider : IEventProvider
    {
        private readonly ConductorOptions _options;
        public OptionsEventProvider(IOptions<ConductorOptions> options) => _options = options.Value;
        public ValueTask<IEnumerable<EventDefinition>> GetEventsAsync(CancellationToken cancellationToken) => new(_options.Events);
    }
}