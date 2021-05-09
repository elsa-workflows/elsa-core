# elsa-confirm-dialog



<!-- Auto Generated Below -->


## Methods

### `hide() => Promise<void>`



#### Returns

Type: `Promise<void>`



### `show(caption: string, message: string) => Promise<boolean>`



#### Returns

Type: `Promise<boolean>`




## Dependencies

### Used by

 - [elsa-workflow-definitions-list-screen](../../screens/workflow-definition-list/elsa-workflow-definitions-screen)
 - [elsa-workflow-instance-list-screen](../../screens/workflow-instance-list/elsa-workflow-instance-list-screen)

### Depends on

- [elsa-modal-dialog](../elsa-modal-dialog)

### Graph
```mermaid
graph TD;
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-workflow-definitions-list-screen --> elsa-confirm-dialog
  elsa-workflow-instance-list-screen --> elsa-confirm-dialog
  style elsa-confirm-dialog fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
