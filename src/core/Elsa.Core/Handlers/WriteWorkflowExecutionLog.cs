using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Events;
using Elsa.Models;
using Elsa.Services.Models;
using MediatR;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Elsa.Handlers
{
    public class WriteWorkflowExecutionLog : INotificationHandler<ActivityExecuting>, INotificationHandler<ActivityExecutionResultExecuted>, INotificationHandler<ActivityFaulted>
    {
        private readonly IMapper _mapper;
        private readonly JsonSerializer _jsonSerializer;

        public WriteWorkflowExecutionLog(IMapper mapper, JsonSerializer jsonSerializer)
        {
            _mapper = mapper;
            _jsonSerializer = jsonSerializer;
        }

        public Task Handle(ActivityExecuting notification, CancellationToken cancellationToken)
        {
            WriteEntry(notification.Resuming ? "Resuming" : "Executing", default, notification.ActivityExecutionContext, null);
            return Task.CompletedTask;
        }

        public Task Handle(ActivityExecutionResultExecuted notification, CancellationToken cancellationToken)
        {
            var activityExecutionContext = notification.ActivityExecutionContext;

            var data = new JObject
            {
                ["Outcomes"] = JToken.FromObject(activityExecutionContext.Outcomes, _jsonSerializer)
            };

            foreach (var entry in activityExecutionContext.JournalData)
                data[entry.Key] = entry.Value != null ? JToken.FromObject(entry.Value, _jsonSerializer) : JValue.CreateNull();

            var resuming = activityExecutionContext.Resuming;
            WriteEntry(resuming ? "Resumed" : "Executed", default, activityExecutionContext, data);
            return Task.CompletedTask;
        }

        public Task Handle(ActivityFaulted notification, CancellationToken cancellationToken)
        {
            var exception = notification.Exception;
            var exceptionModel = _mapper.Map<SimpleException>(exception);

            var data = new
            {
                Exception = exceptionModel
            };

            WriteEntry("Faulted", exception.Message, notification.ActivityExecutionContext, data);
            return Task.CompletedTask;
        }

        private void WriteEntry(string eventName, string? message, ActivityExecutionContext activityExecutionContext, object? data) => activityExecutionContext.AddEntry(eventName, message, data);
    }
}