import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDesignDisplayContext, EventTypes, SyntaxNames} from "../models";
import {h} from "@stencil/core";
import {parseJson} from "../utils/utils";
import {SwitchCase} from "../components/editors/properties/elsa-switch-cases-property/models";

export class SwitchPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    const activityModel = context.activityModel;
    
    if (activityModel.type !== 'Switch')
      return;

    const props = activityModel.properties || [];
    const syntax = 'Switch';
    const casesProp = props.find(x => x.name == 'Cases') || { expressions: {'Switch': ''}, syntax: syntax };
    const expression = casesProp.expressions[syntax] || '[]';
    const cases: Array<SwitchCase> = !!expression['$values'] ? expression['$values'] : parseJson(expression) || [];
    context.outcomes = [...cases.map(x => x.name), 'Default'];
  }
}
