# elsa-studio



<!-- Auto Generated Below -->


## Properties

| Property               | Attribute                | Description | Type     | Default     |
| ---------------------- | ------------------------ | ----------- | -------- | ----------- |
| `serverUrl`            | `server-url`             |             | `string` | `undefined` |
| `workflowDefinitionId` | `workflow-definition-id` |             | `string` | `undefined` |


## Dependencies

### Depends on

- [elsa-workflow-definition-editor](../elsa-workflow-definition-editor)

### Graph
```mermaid
graph TD;
  elsa-studio --> elsa-workflow-definition-editor
  elsa-workflow-definition-editor --> elsa-designer-tree
  elsa-workflow-definition-editor --> elsa-activity-picker-modal
  elsa-workflow-definition-editor --> elsa-activity-editor-modal
  elsa-workflow-definition-editor --> elsa-workflow-definition-settings-modal
  elsa-designer-tree --> elsa-designer-tree-activity
  elsa-activity-picker-modal --> elsa-modal-dialog
  elsa-activity-editor-modal --> elsa-modal-dialog
  elsa-workflow-definition-settings-modal --> elsa-modal-dialog
  style elsa-studio fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
