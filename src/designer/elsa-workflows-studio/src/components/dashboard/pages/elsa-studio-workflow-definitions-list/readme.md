# elsa-studio-workflow-definitions-list



<!-- Auto Generated Below -->


## Properties

| Property    | Attribute    | Description | Type            | Default     |
| ----------- | ------------ | ----------- | --------------- | ----------- |
| `history`   | --           |             | `RouterHistory` | `undefined` |
| `serverUrl` | `server-url` |             | `string`        | `undefined` |


## Dependencies

### Depends on

- stencil-route-link
- [elsa-workflow-definitions-list-screen](../../../screens/workflow-definition-list/elsa-workflow-definitions-screen)

### Graph
```mermaid
graph TD;
  elsa-studio-workflow-definitions-list --> stencil-route-link
  elsa-studio-workflow-definitions-list --> elsa-workflow-definitions-list-screen
  elsa-workflow-definitions-list-screen --> stencil-route-link
  elsa-workflow-definitions-list-screen --> elsa-context-menu
  elsa-workflow-definitions-list-screen --> elsa-confirm-dialog
  elsa-confirm-dialog --> elsa-modal-dialog
  style elsa-studio-workflow-definitions-list fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
