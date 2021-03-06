import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDescriptorDisplayContext, ActivityDesignDisplayContext, EventTypes} from "../models";
import {h} from "@stencil/core";
import {parseJson} from "../utils/utils";
import {ScriptIcon} from "../components/icons/script-icon";

export class RunJavascriptPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDescriptorDisplaying, this.onActivityDescriptorDisplaying);
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDescriptorDisplaying(context: ActivityDescriptorDisplayContext) {
    const descriptor = context.activityDescriptor;

    if (descriptor.type !== 'RunJavaScript')
      return;

    context.activityIcon = <ScriptIcon/>;
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'RunJavaScript')
      return;

    const props = activityModel.properties || [];
    const outcomes = props.find(x => x.name == 'Outcomes') || { expression: '' };
    const expression = outcomes.expression;
    const outcomeList = parseJson(expression) || ['Done'];

    context.activityIcon = <ScriptIcon/>;
    context.outcomes = outcomeList;
    context.bodyDisplay = undefined;
  }
}
