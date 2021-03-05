import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDescriptorDisplayContext, ActivityDesignDisplayContext, EventTypes} from "../models";
import {h} from "@stencil/core";
import {WriteLineIcon} from "../components/icons/write-line-icon";

export class WriteLinePlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDescriptorDisplaying, this.onActivityDescriptorDisplaying);
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDescriptorDisplaying(context: ActivityDescriptorDisplayContext) {
    const descriptor = context.activityDescriptor;

    if (descriptor.type !== 'WriteLine')
      return;

    context.activityIcon = <WriteLineIcon/>
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'WriteLine')
      return;

    const props = activityModel.properties || [];
    const condition = props.find(x => x.name == 'Text') || { expression: '' };
    const expression = condition.expression || '';
    context.bodyDisplay = <p>{expression}</p>;
    context.activityIcon = <WriteLineIcon/>
  }
}
