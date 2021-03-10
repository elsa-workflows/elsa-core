import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDesignDisplayContext, EventTypes} from "../models";
import {h} from "@stencil/core";

export class IfPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'If')
      return;

    const props = activityModel.properties || [];
    const condition = props.find(x => x.name == 'Condition') || { expression: '' };
    const expression = condition.expression || '';
    context.bodyDisplay = <p>{expression}</p>;
  }
}
