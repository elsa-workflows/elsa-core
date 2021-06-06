import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDesignDisplayContext, EventTypes, SyntaxNames} from "../models";
import {h} from "@stencil/core";
import {parseJson} from "../utils/utils";

export class RunJavascriptPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;

    if (activityModel.type !== 'RunJavaScript')
      return;

    const props = activityModel.properties || [];
    const outcomes = props.find(x => x.name == 'PossibleOutcomes') || { expressions: {'Json': ''}, syntax: SyntaxNames.Json };
    const expression = outcomes.expressions[SyntaxNames.Json] || '[]';
    context.outcomes = parseJson(expression) || ['Done'];

    if(context.outcomes.length == 0)
      context.outcomes = ['Done'];
  }
}
