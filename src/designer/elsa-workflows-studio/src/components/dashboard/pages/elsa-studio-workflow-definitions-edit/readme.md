# elsa-studio-workflow-definitions-edit



<!-- Auto Generated Below -->


## Properties

| Property | Attribute | Description | Type           | Default     |
| -------- | --------- | ----------- | -------------- | ----------- |
| `match`  | --        |             | `MatchResults` | `undefined` |


## Dependencies

### Depends on

- [elsa-workflow-definition-editor-screen](../../../screens/workflow-definition-editor/elsa-workflow-definition-editor-screen)

### Graph
```mermaid
graph TD;
  elsa-studio-workflow-definitions-edit --> elsa-workflow-definition-editor-screen
  elsa-workflow-definition-editor-screen --> elsa-workflow-fault-information
  elsa-workflow-definition-editor-screen --> elsa-workflow-performance-information
  elsa-workflow-definition-editor-screen --> elsa-tab-header
  elsa-workflow-definition-editor-screen --> elsa-tab-content
  elsa-workflow-definition-editor-screen --> elsa-designer-panel
  elsa-workflow-definition-editor-screen --> elsa-version-history-panel
  elsa-workflow-definition-editor-screen --> elsa-modal-dialog
  elsa-workflow-definition-editor-screen --> elsa-monaco
  elsa-workflow-definition-editor-screen --> elsa-designer-tree
  elsa-workflow-definition-editor-screen --> x6-designer
  elsa-workflow-definition-editor-screen --> elsa-workflow-settings-modal
  elsa-workflow-definition-editor-screen --> elsa-workflow-definition-editor-notifications
  elsa-workflow-definition-editor-screen --> elsa-confirm-dialog
  elsa-workflow-definition-editor-screen --> elsa-activity-picker-modal
  elsa-workflow-definition-editor-screen --> elsa-activity-editor-modal
  elsa-workflow-definition-editor-screen --> elsa-workflow-publish-button
  elsa-workflow-definition-editor-screen --> elsa-flyout-panel
  elsa-workflow-definition-editor-screen --> elsa-workflow-properties-panel
  elsa-workflow-definition-editor-screen --> elsa-workflow-test-panel
  elsa-workflow-definition-editor-screen --> context-consumer
  elsa-version-history-panel --> elsa-context-menu
  elsa-version-history-panel --> elsa-confirm-dialog
  elsa-version-history-panel --> context-consumer
  elsa-confirm-dialog --> elsa-modal-dialog
  elsa-workflow-settings-modal --> elsa-modal-dialog
  elsa-workflow-settings-modal --> elsa-monaco
  elsa-activity-picker-modal --> elsa-modal-dialog
  elsa-activity-editor-modal --> elsa-modal-dialog
  elsa-activity-editor-modal --> elsa-control
  elsa-workflow-publish-button --> context-consumer
  elsa-workflow-properties-panel --> context-consumer
  elsa-workflow-test-panel --> elsa-copy-button
  elsa-workflow-test-panel --> elsa-confirm-dialog
  elsa-workflow-test-panel --> context-consumer
  style elsa-studio-workflow-definitions-edit fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
