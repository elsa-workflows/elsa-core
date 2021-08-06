import {eventBus, ElsaPlugin} from "../services";
import {ActivityDesignDisplayContext, EventTypes, SyntaxNames} from "../models";
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
    const syntax = SyntaxNames.Json;
    const branches = props.find(x => x.name == 'Branches') || {expressions: {'Json': '[]'}, syntax: syntax};
    const expression = branches.expressions[syntax] || [];
    context.outcomes = !!expression['$values'] ? expression['$values'] : Array.isArray(expression) ? expression : parseJson(expression) || [];
  }
}
