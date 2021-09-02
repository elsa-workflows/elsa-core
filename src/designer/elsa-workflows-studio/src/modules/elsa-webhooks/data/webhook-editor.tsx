import {createProviderConsumer} from "@stencil/state-tunnel";
import {h} from "@stencil/core";

export interface WebhookEditorState {
  webhookId: string;
  serverUrl: string;
}

export default createProviderConsumer<WebhookEditorState>(
  {
    webhookId: null,
    serverUrl: null
  },
  (subscribe, child) => (<context-consumer subscribe={subscribe} renderer={child}/>)
);
