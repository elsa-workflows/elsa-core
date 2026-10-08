using Elsa.Workflows.Exceptions;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows;

/// <inheritdoc />
public class OutputConverterInvoker(
    IOutputConverterRegistry registry,
    IOutputConverterSettingsValidator settingsValidator) : IOutputConverterInvoker
{
    /// <inheritdoc />
    public object? Invoke(
        ActivityExecutionContext activityExecutionContext,
        Output output,
        string outputName,
        Type sourceType,
        object value,
        OutputBindingDestination destination)
    {
        var configuration = output.Converter ?? throw new InvalidOperationException("The output binding has no converter configuration.");
        var descriptor = registry.Find(configuration.Id);

        if (descriptor == null)
            throw CreateException(OutputConversionFailureStage.Resolution);

        if (!IsRuntimeValueCompatible(value, sourceType) || !descriptor.SourceType.IsAssignableFrom(sourceType))
            throw CreateException(OutputConversionFailureStage.SourceCompatibility);

        if (!OutputConverterRegistry.IsAssignableToDestination(descriptor.ResultType, destination.Type))
            throw CreateException(OutputConversionFailureStage.ResultValidation);

        IOutputConverter converter;

        try
        {
            converter = activityExecutionContext.WorkflowExecutionContext.ServiceProvider
                .GetRequiredKeyedService<IOutputConverter>(configuration.Id);
        }
        catch (Exception e)
        {
            throw CreateException(OutputConversionFailureStage.Resolution, e);
        }

        IReadOnlyCollection<string> settingsErrors;

        try
        {
            settingsErrors = settingsValidator.Validate(descriptor, converter, configuration.Settings);
        }
        catch (Exception e)
        {
            throw CreateException(OutputConversionFailureStage.SettingsValidation, e);
        }

        if (settingsErrors.Count > 0)
            throw CreateException(OutputConversionFailureStage.SettingsValidation);

        object? convertedValue;

        try
        {
            convertedValue = converter.Convert(new(value, sourceType, destination.Type, configuration.Settings));
        }
        catch (Exception e)
        {
            throw CreateException(OutputConversionFailureStage.Invocation, e);
        }

        if (convertedValue == null)
        {
            if (!destination.AllowsNull)
                throw CreateException(OutputConversionFailureStage.ResultValidation);

            return null;
        }

        if (!IsRuntimeValueCompatible(convertedValue, descriptor.ResultType) ||
            !IsRuntimeValueCompatible(convertedValue, destination.Type))
        {
            throw CreateException(OutputConversionFailureStage.ResultValidation);
        }

        return convertedValue;

        OutputConversionException CreateException(OutputConversionFailureStage stage, Exception? innerException = null) =>
            new(
                configuration.Id,
                stage,
                activityExecutionContext.Activity.Id,
                activityExecutionContext.Activity.Type,
                outputName,
                destination.Id,
                sourceType,
                destination.Type,
                innerException);
    }

    private static bool IsRuntimeValueCompatible(object value, Type declaredType)
    {
        if (declaredType.IsInstanceOfType(value))
            return true;

        return Nullable.GetUnderlyingType(declaredType)?.IsInstanceOfType(value) == true;
    }
}
