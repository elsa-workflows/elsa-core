# elsa-workflow-definition-editor



<!-- Auto Generated Below -->


## Properties

| Property               | Attribute                | Description | Type     | Default     |
| ---------------------- | ------------------------ | ----------- | -------- | ----------- |
| `serverUrl`            | `server-url`             |             | `string` | `undefined` |
| `workflowDefinitionId` | `workflow-definition-id` |             | `string` | `undefined` |


## Dependencies

### Depends on

- [elsa-designer-tree](../../designers/tree/elsa-designer-tree)
- [elsa-activity-picker-modal](../../pickers/elsa-activity-picker-modal)
- [elsa-activity-editor-modal](../elsa-activity-editor-modal)
- [elsa-workflow-settings-modal](../elsa-workflow-settings-modal)
- [elsa-workflow-publish-button](../elsa-workflow-publish-button)

### Graph
```mermaid
graph TD;
  elsa-workflow-editor --> elsa-designer-tree
  elsa-workflow-editor --> elsa-activity-picker-modal
  elsa-workflow-editor --> elsa-activity-editor-modal
  elsa-workflow-editor --> elsa-workflow-settings-modal
  elsa-workflow-editor --> elsa-workflow-publish-button
  elsa-designer-tree --> elsa-designer-tree-activity
  elsa-activity-picker-modal --> elsa-modal-dialog
  elsa-activity-editor-modal --> elsa-modal-dialog
  elsa-workflow-settings-modal --> elsa-modal-dialog
  style elsa-workflow-editor fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
