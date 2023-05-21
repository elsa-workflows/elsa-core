using System.Reflection;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.ActivityInputOptions;

namespace Elsa.JavaScript.Activities;

internal class RunJavaScriptOptionsProvider : IActivityPropertyOptionsProvider
{
    public ValueTask<object> GetOptionsAsync(PropertyInfo property, CancellationToken cancellationToken = default)
    {
        return new(new CodeEditorOptions
        {
            Language = "javascript"
        });
    }
}