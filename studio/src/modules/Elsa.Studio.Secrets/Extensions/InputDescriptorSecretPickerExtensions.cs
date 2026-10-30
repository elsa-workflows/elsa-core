using System.Text.Json;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Secrets.Models;

namespace Elsa.Studio.Secrets.Extensions;

public static class InputDescriptorSecretPickerExtensions
{
    public static SecretPickerOptions GetSecretPickerOptions(this InputDescriptor descriptor)
    {
        var specifications = descriptor.UISpecifications;
        if (specifications == null)
            return new SecretPickerOptions();

        var props = specifications.FirstOrDefault(x =>
            string.Equals(x.Key, SecretInputUIHints.SecretPicker, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(x.Key, nameof(SecretPickerOptions), StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(props.Key))
            return new SecretPickerOptions();

        return DeserializeOptions(props.Value);
    }

    private static SecretPickerOptions DeserializeOptions(object? value)
    {
        if (value == null)
            return new SecretPickerOptions();

        if (value is JsonElement { ValueKind: JsonValueKind.Undefined or JsonValueKind.Null })
            return new SecretPickerOptions();

        if (value is JsonElement element)
            return element.Deserialize(SecretJsonSerializerContext.Default.SecretPickerOptions) ?? new SecretPickerOptions();

        if (value is string text)
            return string.IsNullOrWhiteSpace(text) ? new SecretPickerOptions() : JsonSerializer.Deserialize(text, SecretJsonSerializerContext.Default.SecretPickerOptions) ?? new SecretPickerOptions();

        return value as SecretPickerOptions ?? new SecretPickerOptions();
    }
}
