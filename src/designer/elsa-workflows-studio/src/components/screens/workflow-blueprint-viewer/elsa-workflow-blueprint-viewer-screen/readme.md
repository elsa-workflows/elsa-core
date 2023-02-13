# elsa-workflow-instance-viewer-screen



<!-- Auto Generated Below -->


## Properties

| Property               | Attribute                | Description | Type     | Default     |
| ---------------------- | ------------------------ | ----------- | -------- | ----------- |
| `culture`              | `culture`                |             | `string` | `undefined` |
| `serverUrl`            | `server-url`             |             | `string` | `undefined` |
| `workflowDefinitionId` | `workflow-definition-id` |             | `string` | `undefined` |


## Methods

### `getServerUrl() => Promise<string>`



#### Returns

Type: `Promise<string>`




## Dependencies

### Used by

 - [elsa-studio-workflow-blueprint-view](../../../dashboard/pages/elsa-studio-workflow-blueprint-view)

### Depends on

- [elsa-designer-tree](../../../designers/tree/elsa-designer-tree)
- [x6-designer](../../../designers/x6-designer)
- [elsa-flyout-panel](../../../shared/elsa-flyout-panel)
- [elsa-tab-header](../../../shared/elsa-tab-header)
- [elsa-tab-content](../../../shared/elsa-tab-content)
- [elsa-workflow-blueprint-properties-panel](../elsa-workflow-blueprint-properties-panel)
- context-consumer

### Graph
```mermaid
graph TD;
  elsa-workflow-blueprint-viewer-screen --> elsa-designer-tree
  elsa-workflow-blueprint-viewer-screen --> x6-designer
  elsa-workflow-blueprint-viewer-screen --> elsa-flyout-panel
  elsa-workflow-blueprint-viewer-screen --> elsa-tab-header
  elsa-workflow-blueprint-viewer-screen --> elsa-tab-content
  elsa-workflow-blueprint-viewer-screen --> elsa-workflow-blueprint-properties-panel
  elsa-workflow-blueprint-viewer-screen --> context-consumer
  elsa-workflow-blueprint-properties-panel --> context-consumer
  elsa-studio-workflow-blueprint-view --> elsa-workflow-blueprint-viewer-screen
  style elsa-workflow-blueprint-viewer-screen fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
