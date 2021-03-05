import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDescriptorDisplayContext, ActivityDesignDisplayContext, EventTypes} from "../models";
import {h} from "@stencil/core";
import {HttpEndpointIcon} from "../components/icons/http-endpoint-icon";

export class HttpEndpointPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDescriptorDisplaying, this.onActivityDescriptorDisplaying);
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDisplaying);
  }

  onActivityDescriptorDisplaying(context: ActivityDescriptorDisplayContext) {
    const descriptor = context.activityDescriptor;

    if (descriptor.type !== 'HttpEndpoint')
      return;

    context.activityIcon = <HttpEndpointIcon/>
  }

  onActivityDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'HttpEndpoint')
      return;

    const props = activityModel.properties || [];
    const path = props.find(x => x.name == 'Path') || { expression: '' };
    context.bodyDisplay = <p>{path.expression}</p>;
    context.activityIcon = <HttpEndpointIcon/>
  }
}
