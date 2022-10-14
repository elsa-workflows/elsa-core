# elsa-studio-dashboard



<!-- Auto Generated Below -->


## Properties

| Property   | Attribute   | Description | Type     | Default     |
| ---------- | ----------- | ----------- | -------- | ----------- |
| `basePath` | `base-path` |             | `string` | `''`        |
| `culture`  | `culture`   |             | `string` | `undefined` |


## Dependencies

### Depends on

- stencil-route-link
- stencil-route
- [elsa-user-context-menu](../../../controls/elsa-user-context-menu)
- stencil-router
- stencil-route-switch
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-studio-dashboard --> stencil-route-link
  elsa-studio-dashboard --> stencil-route
  elsa-studio-dashboard --> elsa-user-context-menu
  elsa-studio-dashboard --> stencil-router
  elsa-studio-dashboard --> stencil-route-switch
  elsa-studio-dashboard --> context-consumer
  elsa-user-context-menu --> elsa-dropdown-button
  elsa-user-context-menu --> context-consumer
  elsa-dropdown-button --> stencil-route-link
  style elsa-studio-dashboard fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
