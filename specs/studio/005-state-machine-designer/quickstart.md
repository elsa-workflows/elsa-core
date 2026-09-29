# Quickstart: State Machine Designer

## Prerequisites

- Run Studio from the `release/3.8.0` line with a 3.8 backend that advertises `Elsa.StateMachine`.
- Confirm the activity library is available. Transition activity selection uses the same backend activity descriptors as the rest of Studio.

## Create a state machine

1. Open **Workflows → Definitions** and select **Create workflow**.
2. Choose **State machine**, give the workflow a name, and select **OK**.
3. Use **Add** to create states and transitions.
4. Set the initial state and select a transition route to open its inspector.

The transition inspector reads top to bottom as the runtime executes it:

1. **WHEN** — the optional Trigger activity. With no Trigger, the transition is evaluated immediately after source Entry.
2. **ONLY IF** — the optional Boolean Condition. With no Condition, the transition is always eligible.
3. **THEN** — the optional Action activity, run after source Exit.
4. **TO** — the target state.

## Configure Trigger and Action activities

1. Select **Add trigger** or **Add action**.
2. Search the activity library by display name, type, category, or description, then choose an activity.
3. Configure the selected activity with the normal activity property editor.
4. Use **Open** to edit the configured activity, **Replace** to choose another activity, or **Clear** to remove it.

Replacing is transactional: canceling the picker leaves the original activity unchanged. Dragging an activity from the activity library onto an empty or configured slot follows the same add/replace path. A transition slot contains one activity. For multiple Action steps, choose the promoted **Sequence** option and select **Open** to edit its children in the regular Sequence designer.

Unavailable or malformed activity definitions remain visible and are preserved until they are explicitly replaced or cleared. Read-only workflows keep **Open** for inspectable activities but suppress mutation controls and ignore drops.

## Configure a Condition

1. Select **Edit** in **ONLY IF**. The wide condition workspace opens without changing the saved definition.
2. Choose one mode:
   - **Always** removes the Condition slot, so the runtime treats it as true.
   - **Never** stores an explicit false condition.
   - **Expression** stores a Boolean expression using an available provider such as JavaScript, Python, or Liquid.
   - **Custom JSON** is the advanced escape hatch for definitions that do not map to a known provider.
3. Select **Apply** to commit the change, or **Cancel** to preserve the original value byte-for-byte at its owning node.

An existing explicit true expression is not silently normalized to a missing Condition. Switching away from an unknown or lossy representation shows a replacement warning. Invalid custom JSON disables **Apply** and leaves the original source intact.

## Save, reload, and inspect

1. Save the workflow and reload the definition.
2. Verify each state and transition route is present once.
3. Reopen each transition and verify Trigger, Condition, Action, and destination summaries.
4. Open a Sequence Action and verify its nested children remain attached to the same transition slot.
5. Open the workflow read-only and verify activity inspection remains available while Add, Replace, Clear, Edit, Delete, and drop mutation are unavailable.

## Broken and unavailable definitions

- A missing state reference is a blocking validation issue. Repair the target or remove the transition before export/publish.
- Unknown providers, unavailable activities, unknown properties, and malformed source text are preserved on a no-op save.
- Status is communicated in text (for example **Always**, **Never**, **Unavailable**, or **Invalid**), not by color alone.

## Validation commands

```bash
dotnet test src/modules/Elsa.Studio.Workflows.Tests/Elsa.Studio.Workflows.Tests.csproj --no-restore
dotnet test src/modules/Elsa.Studio.Workflows.Designer.Tests/Elsa.Studio.Workflows.Designer.Tests.csproj --no-restore
```

The Workflows suite validates the inspector, picker, condition workspace, read-only behavior, and slot ownership. The Designer suite validates mapper/session preservation and runs on .NET 8, 9, and 10.
