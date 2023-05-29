# elsa-confirm-dialog



<!-- Auto Generated Below -->


## Properties

| Property  | Attribute | Description | Type     | Default     |
| --------- | --------- | ----------- | -------- | ----------- |
| `culture` | `culture` |             | `string` | `undefined` |


## Methods

### `hide() => Promise<void>`



#### Returns

Type: `Promise<void>`



### `show(caption: string, message: string) => Promise<boolean>`



#### Returns

Type: `Promise<boolean>`




## Dependencies

### Used by

 - [elsa-credential-manager-list-screen](../../../modules/credential-manager/components)
 - [elsa-studio-root](../../dashboard/pages/elsa-studio-root)
 - [elsa-version-history-panel](../../screens/workflow-definition-editor/elsa-version-history-panel)
 - [elsa-webhook-definitions-list-screen](../../../modules/elsa-webhooks/components/screens/webhook-definition-list/else-webhook-definitions-screen)
 - [elsa-workflow-definition-editor-screen](../../screens/workflow-definition-editor/elsa-workflow-definition-editor-screen)
 - [elsa-workflow-definitions-list-screen](../../screens/workflow-definition-list/elsa-workflow-definitions-screen)
 - [elsa-workflow-registry-list-screen](../../screens/workflow-registry-list/elsa-workflow-registry-list-screen)
 - [elsa-workflow-test-panel](../../screens/workflow-definition-editor/elsa-workflow-test-panel)

### Depends on

- [elsa-modal-dialog](../elsa-modal-dialog)

### Graph
```mermaid
graph TD;
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-credential-manager-list-screen --> elsa-confirm-dialog
  elsa-studio-root --> elsa-confirm-dialog
  elsa-version-history-panel --> elsa-confirm-dialog
  elsa-webhook-definitions-list-screen --> elsa-confirm-dialog
  elsa-workflow-definition-editor-screen --> elsa-confirm-dialog
  elsa-workflow-definitions-list-screen --> elsa-confirm-dialog
  elsa-workflow-registry-list-screen --> elsa-confirm-dialog
  elsa-workflow-test-panel --> elsa-confirm-dialog
  style elsa-confirm-dialog fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
