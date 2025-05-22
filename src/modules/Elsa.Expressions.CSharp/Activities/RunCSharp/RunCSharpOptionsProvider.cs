using System.Reflection;
using Elsa.Workflows.UIHints.CodeEditor;

// ReSharper disable once CheckNamespace
namespace Elsa.Expressions.CSharp.Activities;

internal class RunCSharpOptionsProvider : CodeEditorOptionsProviderBase
{
    protected override string GetLanguage(PropertyInfo propertyInfo, object? context) => "csharp";
}