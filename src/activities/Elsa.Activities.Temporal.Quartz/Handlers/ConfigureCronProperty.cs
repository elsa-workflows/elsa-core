using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Events;
using MediatR;

namespace Elsa.Activities.Temporal.Quartz.Handlers
{
    public class ConfigureCronProperty : INotificationHandler<DescribingActivityType>
    {
        public Task Handle(DescribingActivityType notification, CancellationToken cancellationToken)
        {
            var activityType = notification.ActivityType;

            if (activityType.Type != typeof(Cron))
                return Task.CompletedTask;

            var cronExpressionProperty = notification.ActivityDescriptor.InputProperties.First(x => x.Name == nameof(Cron.CronExpression));
            cronExpressionProperty.DefaultValue = "0 * 0 ? * * *";
            cronExpressionProperty.Hint = "Specify a Quartz CRON expression. Go to https://www.freeformatter.com/cron-expression-generator-quartz.html to generate valid Quartz cron expressions.";
            
            return Task.CompletedTask;
        }
    }
}