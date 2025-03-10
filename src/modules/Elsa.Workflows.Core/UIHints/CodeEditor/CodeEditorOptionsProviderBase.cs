using System.Reflection;

namespace Elsa.Workflows.UIHints.CodeEditor;

/// <summary>
/// Provides options for the code editor component.
/// </summary>
public abstract class CodeEditorOptionsProviderBase : IPropertyUIHandler
{
    /// <inheritdoc />
    public ValueTask<IDictionary<string, object>> GetUIPropertiesAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken = default)
    {
        var codeEditorOptions = GetCodeEditorOptions(propertyInfo, context);

        var options = new Dictionary<string, object>
        {
            [InputUIHints.CodeEditor] = codeEditorOptions
        };

        return new(options);
    }

    /// <summary>
    /// Returns an object containing properties that will be used to render the UI for the property.
    /// </summary>
    protected virtual CodeEditorOptions GetCodeEditorOptions(PropertyInfo propertyInfo, object? context)
    {
        var language = GetLanguage(propertyInfo, context);
        var options = new CodeEditorOptions();

        if (!string.IsNullOrWhiteSpace(language))
            options.Language = language;

        return options;
    }

    /// <summary>
    /// Returns the language to use for syntax highlighting.
    /// </summary>
    protected virtual string GetLanguage(PropertyInfo propertyInfo, object? context) => "";
}