# elsa-workflow-properties-panel



<!-- Auto Generated Below -->


## Properties

| Property     | Attribute     | Description | Type     | Default     |
| ------------ | ------------- | ----------- | -------- | ----------- |
| `culture`    | `culture`     |             | `string` | `undefined` |
| `serverUrl`  | `server-url`  |             | `string` | `undefined` |
| `workflowId` | `workflow-id` |             | `string` | `undefined` |


## Dependencies

### Used by

 - [elsa-workflow-blueprint-viewer-screen](../elsa-workflow-blueprint-viewer-screen)

### Depends on

- context-consumer

### Graph
```mermaid
graph TD;
  elsa-workflow-blueprint-properties-panel --> context-consumer
  elsa-workflow-blueprint-viewer-screen --> elsa-workflow-blueprint-properties-panel
  style elsa-workflow-blueprint-properties-panel fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
