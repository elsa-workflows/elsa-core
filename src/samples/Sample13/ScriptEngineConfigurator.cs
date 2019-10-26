using System;
using Elsa.Scripting;
using Elsa.Scripting.JavaScript;
using Elsa.Services.Models;
using Jint;
using NodaTime;

namespace Sample13
{
    /// <summary>
    /// Add custom JavaScript functions that are easy to use in workflow expressions.
    /// </summary>
    public class ScriptEngineConfigurator : IScriptEngineConfigurator
    {
        private readonly IClock clock;

        public ScriptEngineConfigurator(IClock clock)
        {
            this.clock = clock;
        }

        public void Configure(Engine engine, WorkflowExecutionContext workflowExecutionContext)
        {
            engine.SetValue("getDateOfBirth", (Func<string, object>) (age => clock.GetCurrentInstant().Minus(Duration.FromDays(int.Parse(age) * 365)).ToDateTimeUtc().Year));
        }
    }
}