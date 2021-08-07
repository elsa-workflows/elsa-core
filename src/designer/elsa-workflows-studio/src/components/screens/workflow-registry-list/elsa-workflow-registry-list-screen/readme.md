# elsa-workflow-registry-list-screen



<!-- Auto Generated Below -->


## Properties

| Property    | Attribute    | Description | Type            | Default     |
| ----------- | ------------ | ----------- | --------------- | ----------- |
| `basePath`  | `base-path`  |             | `string`        | `undefined` |
| `culture`   | `culture`    |             | `string`        | `undefined` |
| `history`   | --           |             | `RouterHistory` | `undefined` |
| `serverUrl` | `server-url` |             | `string`        | `undefined` |


## Dependencies

### Used by

 - [elsa-studio-workflow-registry](../../../dashboard/pages/elsa-studio-workflow-registry)

### Depends on

- stencil-route-link
- [elsa-context-menu](../../../controls/elsa-context-menu)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-workflow-registry-list-screen --> stencil-route-link
  elsa-workflow-registry-list-screen --> elsa-context-menu
  elsa-workflow-registry-list-screen --> context-consumer
  elsa-studio-workflow-registry --> elsa-workflow-registry-list-screen
  style elsa-workflow-registry-list-screen fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
