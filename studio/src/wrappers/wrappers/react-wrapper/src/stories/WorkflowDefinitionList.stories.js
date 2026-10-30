import {WorkflowDefinitionList} from "../components/WorkflowDefinitionList/WorkflowDefinitionList.jsx";
import {RemoteArgs} from "../remote-args.js";

export default {
    title: "Workflows/WorkflowDefinitionList",
    component: WorkflowDefinitionList,
    parameters: {
        layout: "centered"
    },
    argTypes: {
        definitionId: { control: "text" }
    }
};

export const Default = {
    args: {
        remoteEndpoint: RemoteArgs.remoteEndpoint,
        apiKey: RemoteArgs.apiKey
    }
}