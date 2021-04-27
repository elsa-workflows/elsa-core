# elsa-studio-workflow-registry



<!-- Auto Generated Below -->


## Properties

| Property    | Attribute    | Description | Type            | Default     |
| ----------- | ------------ | ----------- | --------------- | ----------- |
| `history`   | --           |             | `RouterHistory` | `undefined` |
| `serverUrl` | `server-url` |             | `string`        | `undefined` |


## Dependencies

### Depends on

- stencil-route-link
- [elsa-workflow-registry-list-screen](../../../screens/workflow-registry-list/elsa-workflow-registry-list-screen)

### Graph
```mermaid
graph TD;
  elsa-studio-workflow-registry --> stencil-route-link
  elsa-studio-workflow-registry --> elsa-workflow-registry-list-screen
  elsa-workflow-registry-list-screen --> stencil-route-link
  elsa-workflow-registry-list-screen --> elsa-context-menu
  style elsa-studio-workflow-registry fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
