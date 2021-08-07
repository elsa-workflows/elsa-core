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
    const activityDescriptor = context.activityDescriptor;
    const propertyDescriptors = activityDescriptor.inputProperties;
    const switchCaseProperties = propertyDescriptors.filter(x => x.uiHint == 'switch-case-builder');
    
    if (switchCaseProperties.length == 0)
      return;

    let outcomesHash: any = {};
    const syntax = 'Switch';

    for (const propertyDescriptor of switchCaseProperties) {
      const props = activityModel.properties || [];
      const casesProp = props.find(x => x.name == propertyDescriptor.name) || {expressions: {'Switch': ''}, syntax: syntax};

      const expression: any = casesProp.expressions[syntax] || [];
      const cases: Array<SwitchCase> = !!expression['$values'] ? expression['$values'] : Array.isArray(expression) ? expression : parseJson(expression) || [];

      for(const c of cases)
        outcomesHash[c.name] = true;
    }

    const outcomes = Object.keys(outcomesHash);
    context.outcomes = [...outcomes, 'Default'];
  }
}
