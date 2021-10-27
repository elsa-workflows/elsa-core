# elsa-workflow-properties-panel



<!-- Auto Generated Below -->


## Properties

| Property             | Attribute    | Description | Type                 | Default     |
| -------------------- | ------------ | ----------- | -------------------- | ----------- |
| `culture`            | `culture`    |             | `string`             | `undefined` |
| `serverUrl`          | `server-url` |             | `string`             | `undefined` |
| `workflowDefinition` | --           |             | `WorkflowDefinition` | `undefined` |


## Dependencies

### Used by

 - [elsa-workflow-definition-editor-screen](../elsa-workflow-definition-editor-screen)

### Depends on

- context-consumer

### Graph
```mermaid
graph TD;
  elsa-workflow-properties-panel --> context-consumer
  elsa-workflow-definition-editor-screen --> elsa-workflow-properties-panel
  style elsa-workflow-properties-panel fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
