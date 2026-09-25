namespace Elsa.Studio.Alterations.Catalog;

/// <summary>
/// What does the alteration target. Drives drop-target validation and which
/// extra config fields the dialog should collect.
/// </summary>
public enum AlterationTargetKind
{
    /// <summary>The whole workflow instance (e.g. "Cancel").</summary>
    Instance,

    /// <summary>A specific activity within the instance (e.g. "CancelActivity", "ScheduleActivity").</summary>
    Activity,

    /// <summary>A specific variable on the instance (e.g. "ModifyVariable").</summary>
    Variable,
}

/// <summary>
/// What kind of value an alteration config field expects.
/// </summary>
public enum AlterationFieldKind
{
    Text,
    Integer,
    /// <summary>Free-form JSON (rendered as a multi-line text area).</summary>
    Json,
    /// <summary>
    /// Picks a workflow variable defined on the running instance. Renders as a dropdown populated
    /// from <c>IWorkflowInstanceService.GetVariablesAsync(instanceId)</c>; the staged value is the
    /// variable's <c>Id</c> (matches the server-side <c>VariableId</c> field).
    /// </summary>
    VariablePicker,
    /// <summary>
    /// Picks a published version of the running instance's workflow definition. Renders as a
    /// dropdown populated from <c>IWorkflowDefinitionService.ListAsync</c>; the staged value is
    /// the integer version number.
    /// </summary>
    VersionPicker,
}

/// <summary>
/// A single configurable field on an alteration descriptor.
/// </summary>
/// <param name="Key">The JSON property name to write into the alteration object.</param>
/// <param name="DisplayName">Localized label shown in the config dialog.</param>
/// <param name="Kind">How to render and validate the input.</param>
/// <param name="Required">Whether the user must provide a value.</param>
/// <param name="HelperText">Optional helper text shown under the field.</param>
public record AlterationFieldSpec(
    string Key,
    string DisplayName,
    AlterationFieldKind Kind,
    bool Required = true,
    string? HelperText = null);

/// <summary>
/// Static metadata describing one of the built-in alteration types. The descriptor's TypeId
/// matches the server-side alteration type discriminator.
/// </summary>
/// <param name="TypeId">Server-side alteration type id, e.g. "Cancel", "ScheduleActivity".</param>
/// <param name="DisplayName">Human-readable name shown in the palette.</param>
/// <param name="Description">Short tooltip shown on hover.</param>
/// <param name="Target">What the alteration targets.</param>
/// <param name="Icon">A MudBlazor Material icon string.</param>
/// <param name="Color">Optional accent colour token (e.g. "warning", "error").</param>
/// <param name="Fields">Extra config fields the user must fill in before staging.</param>
public record AlterationDescriptor(
    string TypeId,
    string DisplayName,
    string Description,
    AlterationTargetKind Target,
    string Icon,
    string? Color = null,
    IReadOnlyList<AlterationFieldSpec>? Fields = null)
{
    /// <summary>Convenience accessor — never null.</summary>
    public IReadOnlyList<AlterationFieldSpec> ConfigFields => Fields ?? Array.Empty<AlterationFieldSpec>();
}
