using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using Elsa.Secrets.Enrichers;
using MediatR;

namespace Elsa.Secrets.Handlers
{
    public class DescribingActivityTypeHandler : INotificationHandler<DescribingActivityType>
    {
        private readonly IEnumerable<IActivityInputDescriptorEnricher> _enrichers;
        public DescribingActivityTypeHandler(IEnumerable<IActivityInputDescriptorEnricher> enrichers) => _enrichers = enrichers;

        public Task Handle(DescribingActivityType notification, CancellationToken cancellationToken)
        {
            var enrichers = _enrichers.Where(x => x.ActivityType == notification.ActivityType.Type);
            var properties = notification.ActivityType.Type.GetProperties();

            foreach (var enricher in enrichers)
            {
                var descriptor = notification.ActivityDescriptor.InputProperties.FirstOrDefault(x => x.Name == enricher.PropertyName);
                var propertyInfo = properties.FirstOrDefault(x => x.Name == enricher.PropertyName);

                if (descriptor == null || propertyInfo == null) continue;

                enricher.Enrich(descriptor, propertyInfo);
            }

            return Task.CompletedTask;
        }
    }
}