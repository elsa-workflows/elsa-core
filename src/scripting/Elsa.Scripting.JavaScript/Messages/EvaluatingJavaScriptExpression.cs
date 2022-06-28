using Elsa.Services.Models;
using Jint;
using MediatR;

namespace Elsa.Scripting.JavaScript.Messages
{
    public class EvaluatingJavaScriptExpression : INotification
    {
        public EvaluatingJavaScriptExpression(Engine engine, ActivityExecutionContext activityExecutionContext)
        {
            Engine = engine;
            ActivityExecutionContext = activityExecutionContext;
        }

        public Engine Engine { get; }
        public ActivityExecutionContext ActivityExecutionContext { get; }
    }

    public class ConfigureJavaScriptOptions : INotification
    {
        public ConfigureJavaScriptOptions(Jint.Options options, ActivityExecutionContext context)
        {
            JintOptions = options;
            ActivityExecutionContext = context;
        }
        public Jint.Options JintOptions { get; set; }
        public ActivityExecutionContext ActivityExecutionContext { get; }
    }
}