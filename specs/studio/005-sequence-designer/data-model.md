# Data Model: Sequence Designer

## Sequence Activity

Represents an activity with an ordered collection of child activities.

**Fields**

- `id`: Activity identity.
- `nodeId`: Activity node identity within the workflow tree.
- `type`: `Elsa.Sequence`.
- `activities` or equivalent child collection: Ordered Sequence children.
- activity metadata: Existing designer metadata, validation state, and runtime status.
- designer metadata: Selected layout orientation, if set.

**Rules**

- Child order determines execution order.
- Sequence does not persist arbitrary connection edges between children.
- Missing layout metadata must not prevent rendering.
- Missing layout orientation defaults to vertical.

## Sequence Child Activity

Represents a direct child of a Sequence.

**Fields**

- Existing activity JSON fields: `id`, `nodeId`, `name`, `type`, `version`, inputs, outputs, and metadata.
- Existing display metadata used by activity wrappers.
- Optional embedded body/outcome properties.

**Rules**

- Reordering changes only the parent Sequence child collection order.
- Configuration and nested bodies are preserved during reorder, duplicate, copy, paste, and delete operations.
- Activity IDs remain stable during reorder.

## Sequence Order

The authoritative ordered list of child activities.

**State Transitions**

- Add first child: empty collection -> one child.
- Insert child: collection gains a child at a chosen index.
- Reorder child: collection changes index order without changing child configuration.
- Delete child: collection removes the child and clears selection or selects a reasonable neighbor.
- Paste/duplicate: collection inserts cloned children with new IDs/names and preserved nested structure.

## Sequence Designer Metadata

Non-execution metadata owned by the Sequence designer.

**Fields**

- `layoutOrientation`: `vertical` or `horizontal`.

**Rules**

- Missing orientation is treated as `vertical`.
- Orientation affects visual layout only and never changes execution order.
- Per-child positions are derived from order and orientation by default.

## Derived Visual Edge

A visual connector generated from adjacent child order.

**Fields**

- `sourceActivityId`: Previous child ID.
- `targetActivityId`: Next child ID.
- `kind`: Structural Sequence edge.

**Rules**

- Derived edges are not persisted as workflow connections.
- Users cannot create additional persisted Sequence edges.
- Derived edges are recalculated after add, delete, reorder, and reload.
- Manual connection creation is disabled in Sequence mode.

## Embedded Region

A nested body, branch, or named outcome owned by a Sequence child.

**Fields**

- `ownerActivityId`: Activity that owns the region.
- `portName` or region name: The embedded region identifier.
- `activity`: The nested root/container activity.

**Rules**

- Embedded regions are edited through drill-in navigation.
- Embedded regions appear as compact inline previews in the parent Sequence.
- Parent Sequence scope must be restored when navigating back.
- Empty embedded regions remain discoverable.

## Designer Scope

The currently rendered editable region.

**Fields**

- `rootActivity`: Sequence or nested container being edited.
- `parentActivity`: Activity that owns the scope, if any.
- `breadcrumb`: Ordered path of scopes.

**Rules**

- Scope changes preserve parent activity state.
- Selection and updates propagate through existing designer callbacks.
