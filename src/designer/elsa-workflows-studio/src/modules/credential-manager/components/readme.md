# credential-manager-list-screen



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

 - [credential-manager-items-list](../pages)

### Depends on

- [elsa-context-menu](../../../components/controls/elsa-context-menu)
- [elsa-confirm-dialog](../../../components/shared/elsa-confirm-dialog)
- context-consumer

### Graph
```mermaid
graph TD;
  credential-manager-list-screen --> elsa-context-menu
  credential-manager-list-screen --> elsa-confirm-dialog
  credential-manager-list-screen --> context-consumer
  elsa-confirm-dialog --> elsa-modal-dialog
  credential-manager-items-list --> credential-manager-list-screen
  style credential-manager-list-screen fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
