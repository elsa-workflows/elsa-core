using System.Reflection;
using Elsa.Workflows.UIHints.CodeEditor;

// ReSharper disable once CheckNamespace
namespace Elsa.Expressions.Python.Activities;

internal class RunPythonOptionsProvider : CodeEditorOptionsProviderBase
{
    protected override string GetLanguage(PropertyInfo propertyInfo, object? context) => "python";
}