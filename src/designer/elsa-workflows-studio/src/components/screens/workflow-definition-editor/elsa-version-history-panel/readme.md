# elsa-version-history-panel



<!-- Auto Generated Below -->


## Properties

| Property             | Attribute    | Description | Type                 | Default     |
| -------------------- | ------------ | ----------- | -------------------- | ----------- |
| `serverUrl`          | `server-url` |             | `string`             | `undefined` |
| `workflowDefinition` | --           |             | `WorkflowDefinition` | `undefined` |


## Events

| Event                  | Description | Type                                     |
| ---------------------- | ----------- | ---------------------------------------- |
| `deleteVersionClicked` |             | `CustomEvent<WorkflowDefinitionVersion>` |
| `revertVersionClicked` |             | `CustomEvent<WorkflowDefinitionVersion>` |
| `versionSelected`      |             | `CustomEvent<WorkflowDefinitionVersion>` |


## Dependencies

### Used by

 - [elsa-workflow-definition-editor-screen](../elsa-workflow-definition-editor-screen)

### Depends on

- [elsa-context-menu](../../../controls/elsa-context-menu)
- [elsa-confirm-dialog](../../../shared/elsa-confirm-dialog)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-version-history-panel --> elsa-context-menu
  elsa-version-history-panel --> elsa-confirm-dialog
  elsa-version-history-panel --> context-consumer
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-workflow-definition-editor-screen --> elsa-version-history-panel
  style elsa-version-history-panel fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
