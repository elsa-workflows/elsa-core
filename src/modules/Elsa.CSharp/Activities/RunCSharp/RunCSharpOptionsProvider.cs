using System.Reflection;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Management.ActivityInputOptions;

// ReSharper disable once CheckNamespace
namespace Elsa.CSharp.Activities;

internal class RunCSharpOptionsProvider : IActivityPropertyOptionsProvider
{
    public ValueTask<IDictionary<string, object>> GetOptionsAsync(PropertyInfo property, CancellationToken cancellationToken = default)
    {
        var options = new Dictionary<string, object>
        {
            ["CodeEditorOptions"] = new CodeEditorOptions
            {
                Language = "csharp"
            }
        };

        return new(options);
    }
}