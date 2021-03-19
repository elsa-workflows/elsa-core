# elsa-activity-picker-modal



<!-- Auto Generated Below -->


## Properties

| Property             | Attribute | Description | Type                 | Default     |
| -------------------- | --------- | ----------- | -------------------- | ----------- |
| `workflowDefinition` | --        |             | `WorkflowDefinition` | `undefined` |


## Dependencies

### Used by

 - [elsa-workflow-editor](../elsa-workflow-editor)

### Depends on

- [elsa-modal-dialog](../../../shared/elsa-modal-dialog)
- [elsa-monaco](../../monaco/elsa-monaco)

### Graph
```mermaid
graph TD;
  elsa-workflow-settings-modal --> elsa-modal-dialog
  elsa-workflow-settings-modal --> elsa-monaco
  elsa-workflow-editor --> elsa-workflow-settings-modal
  style elsa-workflow-settings-modal fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
