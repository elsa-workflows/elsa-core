# elsa-studio-workflow-instances-view



<!-- Auto Generated Below -->


## Properties

| Property | Attribute | Description | Type           | Default     |
| -------- | --------- | ----------- | -------------- | ----------- |
| `match`  | --        |             | `MatchResults` | `undefined` |


## Dependencies

### Depends on

- [elsa-workflow-instance-viewer-screen](../../../screens/workflow-instance-viewer/elsa-workflow-instance-viewer-screen)

### Graph
```mermaid
graph TD;
  elsa-studio-workflow-instances-view --> elsa-workflow-instance-viewer-screen
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
  style elsa-studio-workflow-instances-view fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
