# elsa-webhook-definitions-list-screen



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

 - [elsa-studio-webhook-definitions-list](../../../dashboard/pages/elsa-studio-webhook-definitions-list)

### Depends on

- stencil-route-link
- [elsa-context-menu](../../../controls/elsa-context-menu)
- [elsa-confirm-dialog](../../../shared/elsa-confirm-dialog)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-webhook-definitions-list-screen --> stencil-route-link
  elsa-webhook-definitions-list-screen --> elsa-context-menu
  elsa-webhook-definitions-list-screen --> elsa-confirm-dialog
  elsa-webhook-definitions-list-screen --> context-consumer
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-studio-webhook-definitions-list --> elsa-webhook-definitions-list-screen
  style elsa-webhook-definitions-list-screen fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
