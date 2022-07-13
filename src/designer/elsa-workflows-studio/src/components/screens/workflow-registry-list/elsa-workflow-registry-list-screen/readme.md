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

- [elsa-context-menu](../../../controls/elsa-context-menu)
- stencil-route-link
- [elsa-pager](../../../controls/elsa-pager)
- [elsa-confirm-dialog](../../../shared/elsa-confirm-dialog)
- [elsa-dropdown-button](../../../controls/elsa-dropdown-button)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-workflow-registry-list-screen --> elsa-context-menu
  elsa-workflow-registry-list-screen --> stencil-route-link
  elsa-workflow-registry-list-screen --> elsa-pager
  elsa-workflow-registry-list-screen --> elsa-confirm-dialog
  elsa-workflow-registry-list-screen --> elsa-dropdown-button
  elsa-workflow-registry-list-screen --> context-consumer
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-dropdown-button --> stencil-route-link
  elsa-studio-workflow-registry --> elsa-workflow-registry-list-screen
  style elsa-workflow-registry-list-screen fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
