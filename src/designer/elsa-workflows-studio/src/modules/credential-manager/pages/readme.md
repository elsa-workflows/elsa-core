# credential-manager-items-list



<!-- Auto Generated Below -->


## Properties

| Property   | Attribute   | Description | Type     | Default     |
| ---------- | ----------- | ----------- | -------- | ----------- |
| `basePath` | `base-path` |             | `string` | `undefined` |
| `culture`  | `culture`   |             | `string` | `undefined` |


## Dependencies

### Depends on

- [credential-manager-list-screen](../components)
- [elsa-secrets-picker-modal](../elsa-secrets-picker-modal)
- [elsa-secret-editor-modal](../elsa-secret-editor-model)
- context-consumer

### Graph
```mermaid
graph TD;
  credential-manager-items-list --> credential-manager-list-screen
  credential-manager-items-list --> elsa-secrets-picker-modal
  credential-manager-items-list --> elsa-secret-editor-modal
  credential-manager-items-list --> context-consumer
  credential-manager-list-screen --> elsa-context-menu
  credential-manager-list-screen --> elsa-confirm-dialog
  credential-manager-list-screen --> context-consumer
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-secrets-picker-modal --> elsa-modal-dialog
  elsa-secret-editor-modal --> elsa-modal-dialog
  elsa-secret-editor-modal --> elsa-control
  style credential-manager-items-list fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
