using System.Text.RegularExpressions;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Validates extension descriptors before they are exposed to configuration or Studio.
/// </summary>
public sealed class ExtensionDescriptorValidator
{
    private static readonly Regex IdentifierPattern = new("^[a-z][a-z0-9]*(?:[-.][a-z0-9]+)*$", RegexOptions.CultureInvariant);
    private static readonly HashSet<string> SupportedValueTypes = new(StringComparer.Ordinal)
    {
        "string", "secret", "boolean", "integer", "number", "uri", "string-array", "json"
    };

    public ExternalAuthenticationAdapterDescriptor Validate(IExternalAuthenticationAdapter extension)
    {
        var descriptor = extension.Describe();
        ValidateDescriptor(extension.Type, descriptor.Type, descriptor.DisplayName, descriptor.Description, descriptor.SettingsVersion, descriptor.Fields, descriptor.CustomEditor);
        return descriptor;
    }

    public UnlinkedIdentityPolicyDescriptor Validate(IUnlinkedIdentityPolicy extension)
    {
        var descriptor = extension.Describe();
        ValidateDescriptor(extension.Type, descriptor.Type, descriptor.DisplayName, descriptor.Description, descriptor.SettingsVersion, descriptor.Fields, descriptor.CustomEditor);
        return descriptor;
    }

    public PermissionGrantSourceDescriptor Validate(IPermissionGrantSource extension)
    {
        var descriptor = extension.Describe();
        ValidateDescriptor(extension.Type, descriptor.Type, descriptor.DisplayName, descriptor.Description, descriptor.SettingsVersion, descriptor.Fields, descriptor.CustomEditor);
        return descriptor;
    }

    private static void ValidateDescriptor(
        string extensionType,
        string descriptorType,
        string displayName,
        string description,
        int settingsVersion,
        IReadOnlyList<SettingFieldDescriptor> fields,
        CustomEditorContract? customEditor)
    {
        var failures = new List<string>();
        if (!IsIdentifier(extensionType))
            failures.Add($"Extension type '{extensionType}' is not a stable identifier.");
        if (!string.Equals(extensionType, descriptorType, StringComparison.Ordinal))
            failures.Add($"Descriptor type '{descriptorType}' does not match extension type '{extensionType}'.");
        if (string.IsNullOrWhiteSpace(displayName))
            failures.Add($"Extension '{extensionType}' must define a display name.");
        if (string.IsNullOrWhiteSpace(description))
            failures.Add($"Extension '{extensionType}' must define a description.");
        if (settingsVersion <= 0)
            failures.Add($"Extension '{extensionType}' must define a positive settings version.");
        if (fields is null)
            failures.Add($"Extension '{extensionType}' must define a fields collection.");
        else
            ValidateFields(extensionType, fields, failures);
        if (customEditor is not null && (!IsIdentifier(customEditor.Key) || customEditor.ContractVersion <= 0))
            failures.Add($"Extension '{extensionType}' has an invalid custom-editor contract.");

        if (failures.Count > 0)
            throw new InvalidOperationException(string.Join(" ", failures));
    }

    private static void ValidateFields(string extensionType, IReadOnlyCollection<SettingFieldDescriptor> fields, ICollection<string> failures)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            if (!IsIdentifier(field.Name))
                failures.Add($"Extension '{extensionType}' has invalid field name '{field.Name}'.");
            else if (!names.Add(field.Name))
                failures.Add($"Extension '{extensionType}' defines field '{field.Name}' more than once.");
            if (string.IsNullOrWhiteSpace(field.DisplayName) || string.IsNullOrWhiteSpace(field.Description))
                failures.Add($"Field '{field.Name}' must define display text.");
            if (!SupportedValueTypes.Contains(field.ValueType))
                failures.Add($"Field '{field.Name}' has unsupported value type '{field.ValueType}'.");
            if (string.IsNullOrWhiteSpace(field.UiHint))
                failures.Add($"Field '{field.Name}' must define a UI hint.");
            if (field.Validation.MinimumLength is < 0 ||
                field.Validation.MaximumLength is < 0 ||
                field.Validation.MinimumLength > field.Validation.MaximumLength)
                failures.Add($"Field '{field.Name}' has invalid length validation.");
            if (field.Validation.Pattern is { Length: > 0 } pattern)
            {
                try
                {
                    _ = new Regex(pattern, RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
                }
                catch (ArgumentException)
                {
                    failures.Add($"Field '{field.Name}' has an invalid validation pattern.");
                }
            }
            if (field.AllowedValues.Any(string.IsNullOrWhiteSpace) ||
                field.AllowedValues.Distinct(StringComparer.Ordinal).Count() != field.AllowedValues.Count)
                failures.Add($"Field '{field.Name}' has invalid or duplicate allowed values.");
            if (field.IsSecretBinding && (!string.Equals(field.ValueType, "secret", StringComparison.Ordinal) || !field.IsRedacted))
                failures.Add($"Secret-binding field '{field.Name}' must use the secret value type and be redacted.");
        }

        foreach (var field in fields.Where(x => x.VisibleWhen is not null))
        {
            var condition = field.VisibleWhen!;
            if (!names.Contains(condition.Field) ||
                string.Equals(condition.Field, field.Name, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(condition.ExpectedValue))
                failures.Add($"Field '{field.Name}' has an invalid visibility condition.");
        }
    }

    private static bool IsIdentifier(string? value) =>
        !string.IsNullOrWhiteSpace(value) && IdentifierPattern.IsMatch(value);
}
