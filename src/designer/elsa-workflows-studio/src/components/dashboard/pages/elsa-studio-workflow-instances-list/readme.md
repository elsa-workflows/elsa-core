# elsa-studio-workflow-instances-list



<!-- Auto Generated Below -->


## Properties

| Property    | Attribute    | Description | Type            | Default     |
| ----------- | ------------ | ----------- | --------------- | ----------- |
| `history`   | --           |             | `RouterHistory` | `undefined` |
| `serverUrl` | `server-url` |             | `string`        | `undefined` |


## Dependencies

### Depends on

- [elsa-workflow-instances-list-screen](../../../screens/workflow-instances-list/elsa-workflow-instances-list-screen)

### Graph
```mermaid
graph TD;
  elsa-studio-workflow-instances-list --> elsa-workflow-instances-list-screen
  elsa-workflow-instances-list-screen --> stencil-route-link
  elsa-workflow-instances-list-screen --> elsa-context-menu
  elsa-workflow-instances-list-screen --> elsa-pager
  elsa-workflow-instances-list-screen --> elsa-confirm-dialog
  elsa-workflow-instances-list-screen --> elsa-dropdown-button
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-dropdown-button --> stencil-route-link
  style elsa-studio-workflow-instances-list fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
