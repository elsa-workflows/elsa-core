using Elsa.Extensions;
using Elsa.Workflows;
using Stubble.Core;
using Stubble.Core.Builders;
using Trimble.Elsa.Activities.Activities;

namespace Trimble.Elsa.Activities;

/// <summary>
/// Contains extension methods for types from the Stubble library.
/// </summary>
internal static class TokenReplacementExtensions
{
    private static StubbleVisitorRenderer _stubble;

    /// <summary>
    /// Primary constructor.
    /// </summary>
    static TokenReplacementExtensions()
    {
        _stubble = new StubbleBuilder()
        .Configure(settings =>
        {
            settings.AddValueGetter(
                typeof(ActivityExecutionContext),
                (activityExecutionContext, variableName, ignoreValue) =>
            {
                var context = activityExecutionContext as ActivityExecutionContext;
                if (context is null)
                {
                    return null;
                }

                var variable = context.ExpressionExecutionContext.GetVariable(variableName);
                if (variable is null)
                {
                    context.LogCritical<ActivityExecutionContext>(
                        $"Tried to get a value for nonexistent variable",
                        new
                        {
                            VariableName = variableName,
                            ActivityType = context.Activity.GetType().Name,
                            ActivityId   = context.Activity.Id
                        });

                    return string.Empty;
                }

                var variableContent = context.Get(variable)?.ToString();
                if (!string.IsNullOrEmpty(variableContent) && variable is EncryptedVariableString)
                {
                    variableContent = variableContent.Decrypt(variable.Name);
                }

                return variableContent;
            });
        })
        .Build();
    }

    /// <summary>
    /// Replaces mustache-style templates containing Elsa variable names with
    /// their values. E.g.  "Bearer {{myTokenVariableName}}" will be replaced
    /// with "Bearer E1l2S3a4S5.568708098"
    /// </summary>
    public static string ReplaceTokens(this string template, ActivityExecutionContext context)
        => _stubble.Render(template, context);

    /// <summary>
    /// Replaces mustache-style templates in every value using Elsa variables.
    /// </summary>
    public static Dictionary<string, string> ReplaceTokens(
        this Dictionary<string, string> templatedDict,
        ActivityExecutionContext context)
    {
        var dict = new Dictionary<string, string>();
        foreach (var item in templatedDict)
        {
            var valueToAdd = item.Value;
            if (valueToAdd is string templatedString)
            {
                valueToAdd = templatedString.ReplaceTokens(context);
            }
            dict.Add(item.Key, valueToAdd);
        }

        return dict;
    }
}
