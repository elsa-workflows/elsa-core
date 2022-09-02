# elsa-context-menu



<!-- Auto Generated Below -->


## Properties

| Property    | Attribute   | Description | Type     | Default     |
| ----------- | ----------- | ----------- | -------- | ----------- |
| `serverUrl` | `serverurl` |             | `string` | `undefined` |


## Dependencies

### Used by

 - [elsa-studio-dashboard](../../dashboard/pages/elsa-studio-dashboard)

### Depends on

- [elsa-dropdown-button](../elsa-dropdown-button)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-user-context-menu --> elsa-dropdown-button
  elsa-user-context-menu --> context-consumer
  elsa-dropdown-button --> stencil-route-link
  elsa-studio-dashboard --> elsa-user-context-menu
  style elsa-user-context-menu fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
