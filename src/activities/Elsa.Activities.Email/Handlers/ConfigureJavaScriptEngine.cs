using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Services;
using Elsa.Services.WorkflowStorage;
using MediatR;
using Microsoft.Extensions.Configuration;
using NodaTime;

namespace Elsa.Activities.Email.Handlers
{
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        private readonly IConfiguration _configuration;
        private readonly IActivityTypeService _activityTypeService;
        private readonly IWorkflowStorageService _workflowStorageService;

        public ConfigureJavaScriptEngine(IConfiguration configuration, IActivityTypeService activityTypeService, IWorkflowStorageService workflowStorageService)
        {
            _configuration = configuration;
            _activityTypeService = activityTypeService;
            _workflowStorageService = workflowStorageService;
        }

        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;
            
            engine.RegisterType<EmailAttachment>();
            return Task.CompletedTask;
        }
    }
}