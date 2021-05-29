using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Http.Services;
using Elsa.Scripting.JavaScript.Events;
using Elsa.Scripting.JavaScript.Messages;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Elsa.Activities.Http.JavaScript
{
    public class ConfigureJavaScriptEngine : INotificationHandler<EvaluatingJavaScriptExpression>, INotificationHandler<RenderingTypeScriptDefinitions>
    {
        private readonly IAbsoluteUrlProvider _absoluteUrlProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ConfigureJavaScriptEngine(
            IAbsoluteUrlProvider absoluteUrlProvider,
            IHttpContextAccessor httpContextAccessor)
        {
            _absoluteUrlProvider = absoluteUrlProvider;
            _httpContextAccessor = httpContextAccessor;
        }

        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;
            var activityExecutionContext = notification.ActivityExecutionContext;

            engine.SetValue(
                "queryString",
                (Func<string, string>)(key => _httpContextAccessor.HttpContext!.Request.Query[key].ToString())
            );
            engine.SetValue(
                "absoluteUrl",
                (Func<string, string>)(url => _absoluteUrlProvider.ToAbsoluteUrl(url).ToString())
            );
            engine.SetValue(
                "signalUrl",
                (Func<string, string>)(signal => activityExecutionContext.GenerateSignalUrl(signal))
            );

            return Task.CompletedTask;
        }

        public Task Handle(RenderingTypeScriptDefinitions notification, CancellationToken cancellationToken)
        {
            var output = notification.Output;

            output.AppendLine("declare function queryString(name: string): string;");
            output.AppendLine("declare function absoluteUrl(url: string): string;");
            output.AppendLine("declare function signalUrl(signal: string): string;");
            
            return Task.CompletedTask;
        }
    }
}