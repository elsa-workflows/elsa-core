import {eventBus, ElsaPlugin} from "../services";
import {ActivityDesignDisplayContext, EventTypes, SyntaxNames} from "../models";
import {h} from "@stencil/core";
import {parseJson} from "../utils/utils";

export class SendHttpRequestPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'SendHttpRequest')
      return;

    const props = activityModel.properties || [];
    const syntax = SyntaxNames.Json;
    const supportedStatusCodes = props.find(x => x.name == 'SupportedStatusCodes') || {expressions: {'Json': '[]'}, syntax: syntax};
    const expression = supportedStatusCodes.expressions[syntax] || [];
    let outcomes = !!expression['$values'] ? expression['$values'] : Array.isArray(expression) ? expression : parseJson(expression) || [];
    
    outcomes = [...outcomes, 'Done', 'Unsupported Status Code']
    
    context.outcomes = outcomes;
  }
}
