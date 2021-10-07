import {ElsaPlugin} from "../services/elsa-plugin";
import {eventBus} from '../services/event-bus';
import {ActivityDesignDisplayContext, EventTypes} from "../models";
import {parseJson} from "../utils/utils";
import {SwitchCase} from "../components/editors/properties/elsa-switch-cases-property/models";
import { consoleTestResultsHandler } from "tslint/lib/test";

export class WorkflowDefinitionPropertyPlugin implements ElsaPlugin {
  constructor() {
    eventBus.on(EventTypes.ActivityDesignDisplaying, this.onActivityDesignDisplaying);
  }

  onActivityDesignDisplaying(context: ActivityDesignDisplayContext) {
    // const activityModel = context.activityModel;
    // const activityDescriptor = context.activityDescriptor;
    // const propertyDescriptors = activityDescriptor.inputProperties;
    // const switchCaseProperties = propertyDescriptors.filter(x => x.uiHint == 'workflow-definition-property-builder');

    // console.log('context', context);
    // if (switchCaseProperties.length == 0)
    //   return;

    // let outcomesHash: any = {};
    // const syntax = 'WorkflowDefinitionProperty';

    // for (const propertyDescriptor of switchCaseProperties) {
    //   const props = activityModel.properties || [];
    //   const casesProp = props.find(x => x.name == propertyDescriptor.name) || {expressions: {'WorkflowDefinitionProperty': ''}, syntax: syntax};

    //   const expression: any = casesProp.expressions[syntax] || [];
    //   const cases: Array<SwitchCase> = !!expression['$values'] ? expression['$values'] : Array.isArray(expression) ? expression : parseJson(expression) || [];

    //   for(const c of cases)
    //     outcomesHash[c.name] = true;
    // }

    // const outcomes = Object.keys(outcomesHash);
    // context.outcomes = [...outcomes, 'Default', 'Done'];
  }
}
