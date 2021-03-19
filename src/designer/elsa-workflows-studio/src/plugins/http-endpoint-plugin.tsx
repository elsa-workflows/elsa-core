import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDesignDisplayContext, EventTypes} from "../models";
import {h} from "@stencil/core";

export class HttpEndpointPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDisplaying);
  }

  onActivityDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'HttpEndpoint')
      return;

    const props = activityModel.properties || [];
    const path = props.find(x => x.name == 'Path') || { expression: '' };
    context.bodyDisplay = <p>{path.expression}</p>;
  }
}
