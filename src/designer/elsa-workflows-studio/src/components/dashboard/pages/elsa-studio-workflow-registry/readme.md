# elsa-studio-workflow-registry



<!-- Auto Generated Below -->


## Properties

| Property   | Attribute   | Description | Type     | Default     |
| ---------- | ----------- | ----------- | -------- | ----------- |
| `basePath` | `base-path` |             | `string` | `undefined` |
| `culture`  | `culture`   |             | `string` | `undefined` |


## Dependencies

### Depends on

- stencil-route-link
- [elsa-workflow-registry-list-screen](../../../screens/workflow-registry-list/elsa-workflow-registry-list-screen)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-studio-workflow-registry --> stencil-route-link
  elsa-studio-workflow-registry --> elsa-workflow-registry-list-screen
  elsa-studio-workflow-registry --> context-consumer
  elsa-workflow-registry-list-screen --> stencil-route-link
  elsa-workflow-registry-list-screen --> elsa-context-menu
  elsa-workflow-registry-list-screen --> context-consumer
  style elsa-studio-workflow-registry fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
