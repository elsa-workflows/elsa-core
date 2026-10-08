using System.Text.Json;

namespace Elsa.Api.Client.Resources.ExternalAuthentication.Descriptors.Models;

public sealed class ExternalAuthenticationAdapterDescriptor
{
    public string Type { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public int SettingsVersion { get; set; }
    public ICollection<ExternalAuthenticationSettingFieldDescriptor> Fields { get; set; } = [];
    public ExternalAuthenticationAdapterCapabilities Capabilities { get; set; } = new();
    public ExternalAuthenticationCustomEditorContract? CustomEditor { get; set; }
}

public sealed class ExternalAuthenticationPolicyDescriptor
{
    public string Type { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public int SettingsVersion { get; set; }
    public ICollection<ExternalAuthenticationSettingFieldDescriptor> Fields { get; set; } = [];
    public ExternalAuthenticationCustomEditorContract? CustomEditor { get; set; }
}

public sealed class ExternalAuthenticationPermissionGrantSourceDescriptor
{
    public string Type { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public int SettingsVersion { get; set; }
    public ICollection<ExternalAuthenticationSettingFieldDescriptor> Fields { get; set; } = [];
    public ExternalAuthenticationCustomEditorContract? CustomEditor { get; set; }
}

/// <summary>
/// One permission resource a claim mapping may be configured to confer.
/// </summary>
/// <remarks>
/// Mirrors the core catalog's descriptor, which is keyed by resource and carries the verbs that resource
/// accepts. It replaced a single <c>Name</c> holding a whole permission string when the two-axis model
/// landed: a mapping picks a resource and a verb, so a client needs both to offer the choice.
/// </remarks>
public sealed class ExternalAuthenticationPermissionDescriptor
{
    /// <summary>The hierarchical resource path, for example <c>workflows/definitions</c>.</summary>
    public string Resource { get; set; } = "";

    /// <summary>The verbs this resource accepts. The wildcard is deliberately absent: it is not a verb a user selects.</summary>
    public ICollection<string> SupportedVerbs { get; set; } = [];

    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";

    /// <summary><c>false</c> for a descriptor auto-registered because a module declared a permission that matched none.</summary>
    public bool Verified { get; set; } = true;
}

public sealed class ExternalAuthenticationSettingFieldDescriptor
{
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string ValueType { get; set; } = "";
    public bool IsRequired { get; set; }
    public string UiHint { get; set; } = "";
    public JsonElement? DefaultValue { get; set; }
    public ICollection<string> AllowedValues { get; set; } = [];
    public ExternalAuthenticationSettingFieldValidation Validation { get; set; } = new();
    public bool IsSecretBinding { get; set; }
    public bool IsUnsafe { get; set; }
    public ExternalAuthenticationSettingFieldVisibilityCondition? VisibleWhen { get; set; }
    public string? HelpText { get; set; }
    public bool IsRedacted { get; set; }
}

public sealed class ExternalAuthenticationSettingFieldValidation
{
    public int? MinimumLength { get; set; }
    public int? MaximumLength { get; set; }
    public string? Pattern { get; set; }
}

public sealed class ExternalAuthenticationSettingFieldVisibilityCondition
{
    public string Field { get; set; } = "";
    public string ExpectedValue { get; set; } = "";
}

public sealed class ExternalAuthenticationCustomEditorContract
{
    public string Key { get; set; } = "";
    public int ContractVersion { get; set; }
}

public sealed class ExternalAuthenticationAdapterCapabilities
{
    public bool SupportsTest { get; set; }
    public bool SupportsPreview { get; set; }
    public bool SupportsUpstreamLogout { get; set; }
}
