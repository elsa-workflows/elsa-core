# elsa-workflow-definitions-list-screen



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

 - [elsa-studio-workflow-definitions-list](../../../dashboard/pages/elsa-studio-workflow-definitions-list)

### Depends on

- stencil-route-link
- [elsa-context-menu](../../../controls/elsa-context-menu)
- [elsa-pager](../../../controls/elsa-pager)
- [elsa-confirm-dialog](../../../shared/elsa-confirm-dialog)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-workflow-definitions-list-screen --> stencil-route-link
  elsa-workflow-definitions-list-screen --> elsa-context-menu
  elsa-workflow-definitions-list-screen --> elsa-pager
  elsa-workflow-definitions-list-screen --> elsa-confirm-dialog
  elsa-workflow-definitions-list-screen --> context-consumer
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-studio-workflow-definitions-list --> elsa-workflow-definitions-list-screen
  style elsa-workflow-definitions-list-screen fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
