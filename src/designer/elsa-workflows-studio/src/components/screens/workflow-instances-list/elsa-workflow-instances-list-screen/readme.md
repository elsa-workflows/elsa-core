# elsa-workflow-instances-list-screen



<!-- Auto Generated Below -->


## Properties

| Property    | Attribute    | Description | Type            | Default     |
| ----------- | ------------ | ----------- | --------------- | ----------- |
| `history`   | --           |             | `RouterHistory` | `undefined` |
| `serverUrl` | `server-url` |             | `string`        | `undefined` |


## Dependencies

### Used by

 - [elsa-studio-workflow-instances-list](../../../dashboard/pages/elsa-studio-workflow-instances-list)

### Depends on

- stencil-route-link
- [elsa-context-menu](../../../controls/elsa-context-menu)
- [elsa-pager](../../../controls/elsa-pager)
- [elsa-confirm-dialog](../../../shared/elsa-confirm-dialog)
- [elsa-dropdown-button](../../../controls/elsa-dropdown-button)

### Graph
```mermaid
graph TD;
  elsa-workflow-instances-list-screen --> stencil-route-link
  elsa-workflow-instances-list-screen --> elsa-context-menu
  elsa-workflow-instances-list-screen --> elsa-pager
  elsa-workflow-instances-list-screen --> elsa-confirm-dialog
  elsa-workflow-instances-list-screen --> elsa-dropdown-button
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-dropdown-button --> stencil-route-link
  elsa-studio-workflow-instances-list --> elsa-workflow-instances-list-screen
  style elsa-workflow-instances-list-screen fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
