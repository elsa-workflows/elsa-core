// Wraps the <elsa-workflow-definition-editor> custom element registered by Elsa Studio's Blazor WebAssembly runtime.
export default function WorkflowDefinitionEditor({definitionId, remoteEndpoint, apiKey, ...props}) {
    return <elsa-workflow-definition-editor {...props} definition-id={definitionId} remote-endpoint={remoteEndpoint} api-key={apiKey}/>;
}
