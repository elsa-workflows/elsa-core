using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Models;
using Elsa.Activities.Http.Services;
using Elsa.Scripting.JavaScript.Messages;
using Elsa.Services.Models;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.Handlers
{
    public class HttpJavaScriptHandler : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        private readonly ITokenService _tokenService;
        private readonly IAbsoluteUrlProvider _absoluteUrlProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public HttpJavaScriptHandler(
            ITokenService tokenService,
            IAbsoluteUrlProvider absoluteUrlProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _tokenService = tokenService;
            _absoluteUrlProvider = absoluteUrlProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;
            var activityExecutionContext = notification.ActivityExecutionContext;

            engine.SetValue(
                "queryString",
                (Func<string, string>)(key => _httpContextAccessor.HttpContext.Request.Query[key].ToString())
            );
            engine.SetValue(
                "absoluteUrl",
                (Func<string, string>)(url => _absoluteUrlProvider.ToAbsoluteUrl(url).ToString())
            );
            engine.SetValue(
                "signalUrl",
                (Func<string, string>)(signal => GenerateUrl(signal, activityExecutionContext))
            );

            return Task.CompletedTask;
        }

        private string GenerateUrl(string signal, ActivityExecutionContext activityExecutionContext)
        {
            var workflowInstanceId =
                activityExecutionContext.WorkflowExecutionContext.WorkflowInstance.Id;
            
            var payload = new Signal(signal, workflowInstanceId);
            var token = _tokenService.CreateToken(payload);
            var url = $"/workflows/signal?token={token}";

            return _absoluteUrlProvider.ToAbsoluteUrl(url).ToString();
        }
    }
}