# elsa-workflow-instance-journal



<!-- Auto Generated Below -->


## Properties

| Property              | Attribute              | Description | Type                   | Default     |
| --------------------- | ---------------------- | ----------- | ---------------------- | ----------- |
| `activityDescriptors` | --                     |             | `ActivityDescriptor[]` | `[]`        |
| `serverUrl`           | `server-url`           |             | `string`               | `undefined` |
| `workflowBlueprint`   | --                     |             | `WorkflowBlueprint`    | `undefined` |
| `workflowInstanceId`  | `workflow-instance-id` |             | `string`               | `undefined` |
| `workflowModel`       | --                     |             | `WorkflowModel`        | `undefined` |


## Events

| Event            | Description | Type                                      |
| ---------------- | ----------- | ----------------------------------------- |
| `recordSelected` |             | `CustomEvent<WorkflowExecutionLogRecord>` |


## Methods

### `hide() => Promise<void>`



#### Returns

Type: `Promise<void>`



### `selectActivityRecord(activityId?: string) => Promise<void>`



#### Returns

Type: `Promise<void>`



### `show() => Promise<void>`



#### Returns

Type: `Promise<void>`




## Dependencies

### Used by

 - [elsa-workflow-instance-viewer-screen](../elsa-workflow-instance-viewer-screen)

### Graph
```mermaid
graph TD;
  elsa-workflow-instance-viewer-screen --> elsa-workflow-instance-journal
  style elsa-workflow-instance-journal fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
