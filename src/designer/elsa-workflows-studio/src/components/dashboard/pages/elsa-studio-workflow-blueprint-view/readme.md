# elsa-studio-workflow-blueprint-view



<!-- Auto Generated Below -->


## Properties

| Property | Attribute | Description | Type           | Default     |
| -------- | --------- | ----------- | -------------- | ----------- |
| `match`  | --        |             | `MatchResults` | `undefined` |


## Dependencies

### Depends on

- [elsa-workflow-blueprint-viewer-screen](../../../screens/workflow-blueprint-viewer/elsa-workflow-blueprint-viewer-screen)

### Graph
```mermaid
graph TD;
  elsa-studio-workflow-blueprint-view --> elsa-workflow-blueprint-viewer-screen
  elsa-workflow-blueprint-viewer-screen --> elsa-designer-tree
  elsa-workflow-blueprint-viewer-screen --> x6-designer
  elsa-workflow-blueprint-viewer-screen --> elsa-flyout-panel
  elsa-workflow-blueprint-viewer-screen --> elsa-tab-header
  elsa-workflow-blueprint-viewer-screen --> elsa-tab-content
  elsa-workflow-blueprint-viewer-screen --> elsa-workflow-blueprint-properties-panel
  elsa-workflow-blueprint-viewer-screen --> context-consumer
  elsa-workflow-blueprint-properties-panel --> context-consumer
  style elsa-studio-workflow-blueprint-view fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
