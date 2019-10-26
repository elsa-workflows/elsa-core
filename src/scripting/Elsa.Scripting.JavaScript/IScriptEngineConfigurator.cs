using Elsa.Services.Models;
using Jint;

namespace Elsa.Scripting
{
    public interface IScriptEngineConfigurator
    {
        void Configure(Engine engine, WorkflowExecutionContext workflowExecutionContext);
    }
}