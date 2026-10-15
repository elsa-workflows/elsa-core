import {WorkflowDefinitionEditor} from "../components/WorkflowDefinitionEditor/WorkflowDefinitionEditor.jsx";
import {RemoteArgs} from "../remote-args.js";

export default {
    title: "Workflows/WorkflowDefinitionEditor",
    component: WorkflowDefinitionEditor,
    parameters: {
        layout: "centered"
    },
    argTypes: {
        definitionId: { control: "text" }
    }
};

export const Default = {
    args: {
        definitionId: "57305c33e237893b",
        remoteEndpoint: RemoteArgs.remoteEndpoint,
        apiKey: RemoteArgs.apiKey
    }
}