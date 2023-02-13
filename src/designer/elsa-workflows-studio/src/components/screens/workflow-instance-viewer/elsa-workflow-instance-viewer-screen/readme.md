# elsa-workflow-instance-viewer-screen



<!-- Auto Generated Below -->


## Properties

| Property             | Attribute              | Description | Type     | Default     |
| -------------------- | ---------------------- | ----------- | -------- | ----------- |
| `culture`            | `culture`              |             | `string` | `undefined` |
| `serverUrl`          | `server-url`           |             | `string` | `undefined` |
| `workflowInstanceId` | `workflow-instance-id` |             | `string` | `undefined` |


## Methods

### `getServerUrl() => Promise<string>`



#### Returns

Type: `Promise<string>`




## Dependencies

### Used by

 - [elsa-studio-workflow-instances-view](../../../dashboard/pages/elsa-studio-workflow-instances-view)

### Depends on

- [elsa-workflow-fault-information](../../../shared/elsa-workflow-debug-information)
- [elsa-workflow-performance-information](../../../shared/elsa-workflow-debug-information)
- [elsa-workflow-instance-journal](../elsa-workflow-instance-journal)
- [elsa-designer-tree](../../../designers/tree/elsa-designer-tree)
- [x6-designer](../../../designers/x6-designer)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-workflow-instance-viewer-screen --> elsa-workflow-fault-information
  elsa-workflow-instance-viewer-screen --> elsa-workflow-performance-information
  elsa-workflow-instance-viewer-screen --> elsa-workflow-instance-journal
  elsa-workflow-instance-viewer-screen --> elsa-designer-tree
  elsa-workflow-instance-viewer-screen --> x6-designer
  elsa-workflow-instance-viewer-screen --> context-consumer
  elsa-workflow-instance-journal --> elsa-copy-button
  elsa-workflow-instance-journal --> elsa-workflow-definition-editor-notifications
  elsa-workflow-instance-journal --> elsa-flyout-panel
  elsa-workflow-instance-journal --> elsa-tab-header
  elsa-workflow-instance-journal --> elsa-tab-content
  elsa-studio-workflow-instances-view --> elsa-workflow-instance-viewer-screen
  style elsa-workflow-instance-viewer-screen fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
