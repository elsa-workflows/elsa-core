using System.Reflection;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.ActivityInputOptions;

namespace Elsa.JavaScript.Activities;

internal class RunJavaScriptOptionsProvider : IActivityPropertyOptionsProvider
{
    public object GetOptions(PropertyInfo property)
    {
        return new CodeEditorOptions
        {
            Language = "javascript"
        };
    }
}