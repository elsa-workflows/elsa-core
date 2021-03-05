import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDescriptorDisplayContext, ActivityDesignDisplayContext, EventTypes} from "../models";
import {h} from "@stencil/core";
import {TimerIcon} from "../components/icons/timer-icon";

export class TimerPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDescriptorDisplaying, this.onActivityDescriptorDisplaying);
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDescriptorDisplaying(context: ActivityDescriptorDisplayContext) {
    const descriptor = context.activityDescriptor;

    if (descriptor.type !== 'Timer')
      return;

    context.activityIcon = <TimerIcon/>;
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'Timer')
      return;

    const props = activityModel.properties || [];
    const condition = props.find(x => x.name == 'Timeout') || { expression: '' };
    const expression = condition.expression || '';
    context.bodyDisplay = <p>{expression}</p>;
    context.activityIcon = <TimerIcon/>;
  }
}
