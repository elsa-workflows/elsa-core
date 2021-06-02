import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";

export interface WebhookEditorState {
  webhookDefinitionId: string;
  serverUrl: string;
}

export default createProviderConsumer<WebhookEditorState>(
  {
    webhookDefinitionId: null,
    serverUrl: null
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
