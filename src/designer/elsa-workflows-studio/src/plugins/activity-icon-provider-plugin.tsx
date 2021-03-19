import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDescriptorDisplayContext, ActivityDesignDisplayContext, EventTypes} from "../models";
import {h} from "@stencil/core";
import { activityIconProvider} from "../services/activity-icon-provider";

export class ActivityIconProviderPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDescriptorDisplaying, this.onActivityDescriptorDisplaying);
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDescriptorDisplaying(context: ActivityDescriptorDisplayContext) {
    const descriptor = context.activityDescriptor;
    const iconEntry = activityIconProvider.getIcon(descriptor.type);

    if (iconEntry)
      context.activityIcon = iconEntry;
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;
    const iconEntry = activityIconProvider.getIcon(activityModel.type);

    if (iconEntry)
      context.activityIcon = iconEntry;
  }
}
