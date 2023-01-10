# elsa-modal-dialog



<!-- Auto Generated Below -->


## Events

| Event    | Description | Type               |
| -------- | ----------- | ------------------ |
| `hidden` |             | `CustomEvent<any>` |
| `shown`  |             | `CustomEvent<any>` |


## Methods

### `hide(animate?: boolean) => Promise<void>`



#### Returns

Type: `Promise<void>`



### `show(animate?: boolean) => Promise<void>`



#### Returns

Type: `Promise<void>`




## Dependencies

### Used by

 - [elsa-activity-editor-modal](../../screens/workflow-definition-editor/elsa-activity-editor-modal)
 - [elsa-activity-picker-modal](../../screens/workflow-definition-editor/elsa-activity-picker-modal)
 - [elsa-confirm-dialog](../elsa-confirm-dialog)
 - [elsa-secret-editor-modal](../../../modules/credential-manager/elsa-secret-editor-modal)
 - [elsa-secrets-picker-modal](../../../modules/credential-manager/elsa-secrets-picker-modal)
 - [elsa-workflow-definition-editor-screen](../../screens/workflow-definition-editor/elsa-workflow-definition-editor-screen)
 - [elsa-workflow-settings-modal](../../screens/workflow-definition-editor/elsa-workflow-settings-modal)

### Graph
```mermaid
graph TD;
  elsa-activity-editor-modal --> elsa-modal-dialog
  elsa-activity-picker-modal --> elsa-modal-dialog
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-secret-editor-modal --> elsa-modal-dialog
  elsa-secrets-picker-modal --> elsa-modal-dialog
  elsa-workflow-definition-editor-screen --> elsa-modal-dialog
  elsa-workflow-settings-modal --> elsa-modal-dialog
  style elsa-modal-dialog fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
