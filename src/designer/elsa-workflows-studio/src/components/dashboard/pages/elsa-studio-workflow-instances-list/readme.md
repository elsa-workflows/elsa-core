# elsa-studio-workflow-instances-list



<!-- Auto Generated Below -->


## Properties

| Property    | Attribute    | Description | Type            | Default     |
| ----------- | ------------ | ----------- | --------------- | ----------- |
| `history`   | --           |             | `RouterHistory` | `undefined` |
| `serverUrl` | `server-url` |             | `string`        | `undefined` |


## Dependencies

### Depends on

- [elsa-workflow-instance-list-screen](../../../screens/workflow-instance-list/elsa-workflow-instance-list-screen)

### Graph
```mermaid
graph TD;
  elsa-studio-workflow-instances-list --> elsa-workflow-instance-list-screen
  elsa-workflow-instance-list-screen --> stencil-route-link
  elsa-workflow-instance-list-screen --> elsa-context-menu
  elsa-workflow-instance-list-screen --> elsa-pager
  elsa-workflow-instance-list-screen --> elsa-confirm-dialog
  elsa-workflow-instance-list-screen --> elsa-dropdown-button
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-dropdown-button --> stencil-route-link
  style elsa-studio-workflow-instances-list fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
