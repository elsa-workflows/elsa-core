# elsa-webhook-settings-modal



<!-- Auto Generated Below -->


## Properties

| Property            | Attribute               | Description | Type                | Default     |
| ------------------- | ----------------------- | ----------- | ------------------- | ----------- |
| `culture`           | `culture`               |             | `string`            | `undefined` |
| `history`           | --                      |             | `RouterHistory`     | `undefined` |
| `serverUrl`         | `server-url`            |             | `string`            | `undefined` |
| `webhookDefinition` | --                      |             | `WebhookDefinition` | `undefined` |
| `webhookId`         | `webhook-definition-id` |             | `string`            | `undefined` |


## Methods

### `getServerUrl() => Promise<string>`



#### Returns

Type: `Promise<string>`



### `getWebhookId() => Promise<string>`



#### Returns

Type: `Promise<string>`




## Dependencies

### Used by

 - [elsa-studio-webhook-definitions-edit](../../../dashboard/pages/elsa-studio-webhook-definitions-edit)

### Depends on

- [elsa-webhook-definition-editor-notifications](../elsa-webhook-definition-editor-notifications)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-webhook-definition-editor-screen --> elsa-webhook-definition-editor-notifications
  elsa-webhook-definition-editor-screen --> context-consumer
  elsa-webhook-definition-editor-notifications --> elsa-toast-notification
  elsa-studio-webhook-definitions-edit --> elsa-webhook-definition-editor-screen
  style elsa-webhook-definition-editor-screen fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
