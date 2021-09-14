# elsa-studio-webhook-definitions-edit



<!-- Auto Generated Below -->


## Properties

| Property | Attribute | Description | Type           | Default     |
| -------- | --------- | ----------- | -------------- | ----------- |
| `match`  | --        |             | `MatchResults` | `undefined` |


## Dependencies

### Depends on

- [elsa-webhook-definition-editor-screen](../../../screens/webhook-definition-editor/elsa-webhook-definition-editor-screen)

### Graph
```mermaid
graph TD;
  elsa-studio-webhook-definitions-edit --> elsa-webhook-definition-editor-screen
  elsa-webhook-definition-editor-screen --> elsa-webhook-definition-editor-notifications
  elsa-webhook-definition-editor-screen --> context-consumer
  elsa-webhook-definition-editor-notifications --> elsa-toast-notification
  style elsa-studio-webhook-definitions-edit fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
