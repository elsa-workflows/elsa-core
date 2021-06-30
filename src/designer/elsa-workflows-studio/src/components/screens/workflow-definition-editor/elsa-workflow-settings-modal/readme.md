# elsa-activity-picker-modal



<!-- Auto Generated Below -->


## Properties

| Property             | Attribute    | Description | Type                 | Default     |
| -------------------- | ------------ | ----------- | -------------------- | ----------- |
| `serverUrl`          | `server-url` |             | `string`             | `undefined` |
| `workflowDefinition` | --           |             | `WorkflowDefinition` | `undefined` |


## Dependencies

### Used by

 - [elsa-workflow-definition-editor-screen](../elsa-workflow-definition-editor-screen)

### Depends on

- [elsa-modal-dialog](../../../shared/elsa-modal-dialog)
- [elsa-monaco](../../../controls/elsa-monaco)

### Graph
```mermaid
graph TD;
  elsa-workflow-settings-modal --> elsa-modal-dialog
  elsa-workflow-settings-modal --> elsa-monaco
  elsa-workflow-definition-editor-screen --> elsa-workflow-settings-modal
  style elsa-workflow-settings-modal fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
