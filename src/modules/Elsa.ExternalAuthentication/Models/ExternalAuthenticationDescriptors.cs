using System.Text.Json;

namespace Elsa.ExternalAuthentication.Models;

public sealed record SettingFieldDescriptor(
    string Name,
    string DisplayName,
    string Description,
    string ValueType,
    bool IsRequired,
    string UiHint,
    JsonElement? DefaultValue,
    IReadOnlyCollection<string> AllowedValues,
    SettingFieldValidation Validation,
    bool IsSecretBinding,
    bool IsUnsafe,
    SettingFieldVisibilityCondition? VisibleWhen,
    string? HelpText,
    bool IsRedacted);

public sealed record SettingFieldValidation(int? MinimumLength = null, int? MaximumLength = null, string? Pattern = null);

public sealed record SettingFieldVisibilityCondition(string Field, string ExpectedValue);

public sealed record CustomEditorContract(string Key, int ContractVersion);

public sealed record ExternalAuthenticationAdapterCapabilities(bool SupportsTest, bool SupportsPreview, bool SupportsUpstreamLogout);

public sealed record ExternalAuthenticationAdapterDescriptor(
    string Type,
    string DisplayName,
    string Description,
    int SettingsVersion,
    IReadOnlyList<SettingFieldDescriptor> Fields,
    ExternalAuthenticationAdapterCapabilities Capabilities,
    CustomEditorContract? CustomEditor);

public sealed record UnlinkedIdentityPolicyDescriptor(string Type, string DisplayName, string Description, int SettingsVersion, IReadOnlyList<SettingFieldDescriptor> Fields, CustomEditorContract? CustomEditor);

public sealed record PermissionGrantSourceDescriptor(string Type, string DisplayName, string Description, int SettingsVersion, IReadOnlyList<SettingFieldDescriptor> Fields, CustomEditorContract? CustomEditor);

public sealed record PermissionDescriptor(string Name, string DisplayName, string Description, string Category);
