import { WorkflowInstanceViewer} from "../components/WorkflowInstanceViewer/WorkflowInstanceViewer.jsx";
import {RemoteArgs} from "../remote-args.js";

export default {
    title: "Workflows/WorkflowInstanceViewer",
    component: WorkflowInstanceViewer,
    parameters: {
        layout: "centered"
    },
    argTypes: {
        definitionId: { control: "text" }
    }
};

export const Default = {
    args: {
        instanceId: "f14afe24a2e6f7b1",
        remoteEndpoint: RemoteArgs.remoteEndpoint,
        apiKey: RemoteArgs.apiKey
    }
}