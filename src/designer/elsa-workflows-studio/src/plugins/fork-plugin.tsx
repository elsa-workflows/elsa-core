import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDesignDisplayContext, EventTypes} from "../models";
import {h} from "@stencil/core";
import {parseJson} from "../utils/utils";

export class ForkPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'Fork')
      return;

    const props = activityModel.properties || [];
    const branches = props.find(x => x.name == 'Branches') || { expressions: {'Literal': ''}, syntax: 'Literal' };
    const expression = branches.expressions[branches.syntax];
    context.outcomes = parseJson(expression) || [];
  }
}
