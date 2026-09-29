using Elsa.Studio.Extensions;
using Elsa.Studio.UIHints.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.UIHints.Extensions;

/// <summary>
/// Provides a set of extension methods for <see cref="IServiceCollection"/>.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default UI hint handlers.
    /// </summary>
    public static IServiceCollection AddDefaultUIHintHandlers(this IServiceCollection services)
    {
        return services
            .AddUIHintHandler<SingleLineHandler>()
            .AddUIHintHandler<CheckboxHandler>()
            .AddUIHintHandler<CheckListHandler>()
            .AddUIHintHandler<DictionaryHandler>()
            .AddUIHintHandler<MultiTextHandler>()
            .AddUIHintHandler<MultiLineHandler>()
            .AddUIHintHandler<DropDownHandler>()
            .AddUIHintHandler<CodeEditorHandler>()
            .AddUIHintHandler<ExpressionEditorHandler>()
            .AddUIHintHandler<JsonEditorHandler>()
            .AddUIHintHandler<SwitchEditorHandler>()
            .AddUIHintHandler<HttpStatusCodesHandler>()
            .AddUIHintHandler<VariablePickerHandler>()
            .AddUIHintHandler<InputPickerHandler>()
            .AddUIHintHandler<TypePickerHandler>()
            .AddUIHintHandler<WorkflowDefinitionPickerHandler>()
            .AddUIHintHandler<OutputPickerHandler>()
            .AddUIHintHandler<RadioListHandler>()
            .AddUIHintHandler<OutcomePickerHandler>()
            .AddUIHintHandler<DynamicOutcomesHandler>()
            .AddUIHintHandler<DateTimePickerHandler>()
            ;
    }
}