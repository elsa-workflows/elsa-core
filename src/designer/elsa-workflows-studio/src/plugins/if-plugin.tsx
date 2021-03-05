import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDescriptorDisplayContext, ActivityDesignDisplayContext, EventTypes} from "../models";
import {h} from "@stencil/core";
import {IfIcon} from "../components/icons/if-icon";

export class IfPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDescriptorDisplaying, this.onActivityDescriptorDisplaying);
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDescriptorDisplaying(context: ActivityDescriptorDisplayContext) {
    const descriptor = context.activityDescriptor;

    if (descriptor.type !== 'If')
      return;

    context.activityIcon = <IfIcon/>
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'If')
      return;

    const props = activityModel.properties || [];
    const condition = props.find(x => x.name == 'Condition') || { expression: '' };
    const expression = condition.expression || '';
    context.bodyDisplay = <p>{expression}</p>;
    context.activityIcon = <IfIcon/>
  }
}
