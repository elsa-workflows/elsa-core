# elsa-dropdown-button



<!-- Auto Generated Below -->


## Properties

| Property | Attribute | Description | Type                                                            | Default                        |
| -------- | --------- | ----------- | --------------------------------------------------------------- | ------------------------------ |
| `icon`   | `icon`    |             | `any`                                                           | `undefined`                    |
| `items`  | --        |             | `DropdownButtonItem[]`                                          | `[]`                           |
| `origin` | `origin`  |             | `DropdownButtonOrigin.TopLeft \| DropdownButtonOrigin.TopRight` | `DropdownButtonOrigin.TopLeft` |
| `text`   | `text`    |             | `string`                                                        | `undefined`                    |


## Events

| Event          | Description | Type                              |
| -------------- | ----------- | --------------------------------- |
| `itemSelected` |             | `CustomEvent<DropdownButtonItem>` |


## Dependencies

### Used by

 - [elsa-workflow-instances-list-screen](../../screens/workflow-instances-list/elsa-workflow-instances-list-screen)

### Depends on

- stencil-route-link

### Graph
```mermaid
graph TD;
  elsa-dropdown-button --> stencil-route-link
  elsa-workflow-instances-list-screen --> elsa-dropdown-button
  style elsa-dropdown-button fill:#f9f,stroke:#333,stroke-width:4px
```

----------------------------------------------

*Built with [StencilJS](https://stenciljs.com/)*
