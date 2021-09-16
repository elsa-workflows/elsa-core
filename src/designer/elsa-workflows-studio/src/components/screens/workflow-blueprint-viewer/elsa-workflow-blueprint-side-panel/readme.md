# elsa-workflow-blueprint-side-panel



<!-- Auto Generated Below -->


## Properties

| Property     | Attribute     | Description | Type     | Default     |
| ------------ | ------------- | ----------- | -------- | ----------- |
| `serverUrl`  | `server-url`  |             | `string` | `undefined` |
| `workflowId` | `workflow-id` |             | `string` | `undefined` |


## Dependencies

### Used by

 - [elsa-workflow-blueprint-viewer-screen](../elsa-workflow-blueprint-viewer-screen)

### Depends on

- [elsa-workflow-blueprint-properties-panel](../elsa-workflow-blueprint-properties-panel)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-workflow-blueprint-side-panel --> elsa-workflow-blueprint-properties-panel
  elsa-workflow-blueprint-side-panel --> context-consumer
  elsa-workflow-blueprint-properties-panel --> context-consumer
  elsa-workflow-blueprint-viewer-screen --> elsa-workflow-blueprint-side-panel
  style elsa-workflow-blueprint-side-panel fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
