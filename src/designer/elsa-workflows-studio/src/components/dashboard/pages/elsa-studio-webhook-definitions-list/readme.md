# elsa-studio-webhook-definitions-list



<!-- Auto Generated Below -->


## Properties

| Property   | Attribute   | Description | Type     | Default     |
| ---------- | ----------- | ----------- | -------- | ----------- |
| `basePath` | `base-path` |             | `string` | `undefined` |
| `culture`  | `culture`   |             | `string` | `undefined` |


## Dependencies

### Depends on

- stencil-route-link
- [elsa-webhook-definitions-list-screen](../../../screens/webhook-definition-list/else-webhook-definitions-screen)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-studio-webhook-definitions-list --> stencil-route-link
  elsa-studio-webhook-definitions-list --> elsa-webhook-definitions-list-screen
  elsa-studio-webhook-definitions-list --> context-consumer
  elsa-webhook-definitions-list-screen --> stencil-route-link
  elsa-webhook-definitions-list-screen --> elsa-context-menu
  elsa-webhook-definitions-list-screen --> elsa-confirm-dialog
  elsa-webhook-definitions-list-screen --> context-consumer
  elsa-confirm-dialog --> elsa-modal-dialog
  style elsa-studio-webhook-definitions-list fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
