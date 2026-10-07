# CustomElements consumer host

This nonpackable host registers the `elsa-workflow-definition-list`,
`elsa-workflow-definition-editor`, `elsa-workflow-instance-list` and
`elsa-workflow-instance-viewer` elements. Set their `remoteEndpoint` and
`accessToken` properties before mounting them. The editor also takes
`definitionId`; the viewer takes `instanceId`.

JavaScript consumers can assign the editor's `definitionExecuted`,
`definitionVersionSelected` and `activitySelectionChanged` callback properties.
Their values are the workflow instance ID, selected workflow definition version
and selected activity, respectively. These EventCallbacks forward the package
editor's notifications. Existing .NET consumers can continue using the
`WorkflowDefinitionExecuted`, `WorkflowDefinitionVersionSelected` and
`ActivitySelected` Func parameters. When both are assigned, the .NET callback
completes before the JavaScript-compatible callback is invoked.

The paired package browser fixture mirrors this host glue explicitly and keeps
Elsa library packages unchanged. Its callback contracts do not substitute for
compiling the host and verifying the complete browser journey.
