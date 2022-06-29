using Elsa.Services.Models;
using MediatR;

namespace Elsa.Scripting.JavaScript.Messages
{
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