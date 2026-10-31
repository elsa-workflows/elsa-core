using MudBlazor;

namespace Elsa.Studio.Alterations.Catalog;

/// <summary>
/// Built-in catalog of the five alteration types known to ship with Elsa Server today.
///
/// The server has no discovery endpoint yet, so this list is hardcoded. When the server adds
/// one, swap this for a remote-fetching <see cref="IAlterationCatalog"/> implementation —
/// the rest of the module reads only through the interface.
/// </summary>
public class AlterationCatalog : IAlterationCatalog
{
    private static readonly IReadOnlyList<AlterationDescriptor> BuiltIns = new AlterationDescriptor[]
    {
        new(
            TypeId: "Cancel",
            DisplayName: "Cancel workflow",
            Description: "Cancels the entire workflow instance.",
            Target: AlterationTargetKind.Instance,
            Icon: Icons.Material.Outlined.Cancel,
            Color: "error"),

        new(
            TypeId: "CancelActivity",
            DisplayName: "Cancel activity",
            Description: "Cancels a specific running activity within the instance.",
            Target: AlterationTargetKind.Activity,
            Icon: Icons.Material.Outlined.HighlightOff,
            Color: "warning"),

        new(
            TypeId: "ScheduleActivity",
            DisplayName: "Schedule activity",
            Description: "Schedules a specific activity for execution within the instance.",
            Target: AlterationTargetKind.Activity,
            Icon: Icons.Material.Outlined.PlayCircleOutline,
            Color: "info"),

        // Server-side fields (Elsa.Alterations.AlterationTypes.ModifyVariable):
        //   VariableId : string  (required)
        //   Value      : object? (any JSON)
        // Server's JsonNamingPolicy is camelCase, so they go on the wire as variableId / value.
        new(
            TypeId: "ModifyVariable",
            DisplayName: "Modify variable",
            Description: "Sets a workflow variable to a new value.",
            Target: AlterationTargetKind.Variable,
            Icon: Icons.Material.Outlined.Edit,
            Color: "primary",
            Fields: new[]
            {
                new AlterationFieldSpec("variableId", "Variable", AlterationFieldKind.VariablePicker, Required: true,
                    HelperText: "Pick a workflow variable from this instance."),
                new AlterationFieldSpec("value", "New value (JSON)", AlterationFieldKind.Json, Required: true,
                    HelperText: "JSON-encoded value, e.g. \"some text\", 42, true, or { \"foo\": 1 }."),
            }),

        new(
            TypeId: "Migrate",
            DisplayName: "Migrate to newer version",
            Description: "Migrates the running instance to a newer published workflow version.",
            Target: AlterationTargetKind.Instance,
            Icon: Icons.Material.Outlined.Upgrade,
            Color: "secondary",
            Fields: new[]
            {
                new AlterationFieldSpec("targetVersion", "Target version", AlterationFieldKind.VersionPicker, Required: true,
                    HelperText: "Pick a published version of this workflow to migrate to."),
            }),
    };

    /// <inheritdoc />
    public ValueTask<IReadOnlyList<AlterationDescriptor>> ListAsync(CancellationToken cancellationToken = default)
        => new(BuiltIns);

    /// <inheritdoc />
    public ValueTask<AlterationDescriptor?> FindAsync(string typeId, CancellationToken cancellationToken = default)
        => new(BuiltIns.FirstOrDefault(x => x.TypeId == typeId));
}
