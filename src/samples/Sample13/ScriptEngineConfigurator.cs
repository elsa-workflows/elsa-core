using System;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Scripting.JavaScript.Messages;
using MediatR;
using NodaTime;

namespace Sample13
{
    /// <summary>
    /// Add custom JavaScript functions that are easy to use in workflow expressions.
    /// </summary>
    public class ScriptEngineConfigurator : INotificationHandler<EvaluatingJavaScriptExpression>
    {
        private readonly IClock clock;

        public ScriptEngineConfigurator(IClock clock)
        {
            this.clock = clock;
        }

        public Task Handle(EvaluatingJavaScriptExpression notification, CancellationToken cancellationToken)
        {
            var engine = notification.Engine;
            engine.SetValue("getDateOfBirth", (Func<string, object>) (age => clock.GetCurrentInstant().Minus(Duration.FromDays(int.Parse(age) * 365)).ToDateTimeUtc().Year));
            return Task.CompletedTask;
        }
    }
}