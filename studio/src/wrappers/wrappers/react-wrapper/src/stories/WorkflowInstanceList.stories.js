import {WorkflowInstanceList} from "../components/WorkflowInstanceList/WorkflowInstanceList.jsx";
import {RemoteArgs} from "../remote-args.js";

export default {
    title: "Workflows/WorkflowInstanceList",
    component: WorkflowInstanceList,
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