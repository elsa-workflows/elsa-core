# elsa-workflow-definition-editor



<!-- Auto Generated Below -->


## Properties

| Property               | Attribute                | Description | Type     | Default     |
| ---------------------- | ------------------------ | ----------- | -------- | ----------- |
| `serverUrl`            | `server-url`             |             | `string` | `undefined` |
| `workflowDefinitionId` | `workflow-definition-id` |             | `string` | `undefined` |


## Dependencies

### Used by

 - [elsa-studio](../elsa-studio)

### Depends on

- [elsa-designer-tree](../../designers/tree/elsa-designer-tree)
- [elsa-activity-picker-modal](../../pickers/modal/elsa-activity-picker-modal)
- [elsa-activity-editor-modal](../../editors/modal/elsa-activity-editor-modal)
- [elsa-workflow-definition-settings-modal](../elsa-workflow-definition-settings-modal)

### Graph
```mermaid
graph TD;
  elsa-workflow-definition-editor --> elsa-designer-tree
  elsa-workflow-definition-editor --> elsa-activity-picker-modal
  elsa-workflow-definition-editor --> elsa-activity-editor-modal
  elsa-workflow-definition-editor --> elsa-workflow-definition-settings-modal
  elsa-designer-tree --> elsa-designer-tree-activity
  elsa-activity-picker-modal --> elsa-modal-dialog
  elsa-activity-editor-modal --> elsa-modal-dialog
  elsa-workflow-definition-settings-modal --> elsa-modal-dialog
  elsa-studio --> elsa-workflow-definition-editor
  style elsa-workflow-definition-editor fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
