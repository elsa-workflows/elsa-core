# Quickstart: Sequence Designer

## Prerequisites

- Current branch: `005-sequence-designer`
- .NET SDK compatible with the repository
- Node/npm available for the designer client bundle

## Build Checks

```bash
dotnet restore Elsa.Studio.sln
dotnet build Elsa.Studio.sln
cd src/modules/Elsa.Studio.Workflows.Designer/ClientLib
npm install
npm run build
```

## Manual Verification

1. Start a Studio host.
2. Enable the enhanced React Flow designer setting if it is not already enabled by default.
3. Create a workflow whose root or nested container is a Sequence.
4. Open the Sequence designer.
5. Verify an empty Sequence shows a clear add/drop target.
6. Add three activities and confirm they render in execution order.
7. Insert a fourth activity between two existing activities.
8. Reorder one activity by dragging and another activity with explicit move commands, then save the workflow.
9. Reload the workflow and confirm visual order and execution order match.
10. Switch the Sequence to horizontal layout, save, reload, and confirm the orientation is restored.
11. Try to create a manual connection and confirm the gesture is disabled.
12. Select a child activity and confirm the existing properties panel updates.
13. Add an activity with an embedded body or named outcome, confirm compact inline previews appear, open the embedded region, edit it, and navigate back through breadcrumbs.
14. Create or load a Sequence with at least 100 children and verify selection, pan/scroll, insert affordances, orientation switching, and save remain usable.
15. Open a workflow instance view and verify execution/status indicators appear on Sequence child activities.
16. Open the same workflow in read-only mode and verify editing actions are disabled while selection and drill-in still work.

## Regression Checks

- Existing Flowchart designer behavior still works with React Flow enabled.
- X6 Flowchart path still builds and renders when React Flow is disabled.
- Fallback designer still handles unsupported activity types.
- Workflows with existing Sequence activities and no Sequence layout metadata open without repair.
