import 'reflect-metadata';
import {h} from "@stencil/core";
import {Container, Service} from "typedi";
import {ActivityDisplayContext, ActivityDriver} from '../../services';
import {isNullOrWhitespace} from "../../utils";

@Service()
export class DefaultActivityDriver implements ActivityDriver {

  display(context: ActivityDisplayContext): any {
    const activityDescriptor = context.activityDescriptor;
    const activityType = activityDescriptor.activityType;
    const activity = context.activity;
    const canStartWorkflow = activity?.canStartWorkflow == true;
    const text = activityDescriptor?.displayName;
    let displayText = activity?.metadata?.displayText;

    if (isNullOrWhitespace(displayText))
      displayText = text;

    const displayType = context.displayType;
    const activityJson = displayType == 'designer' ? encodeURI(JSON.stringify(activity)) : '';

    return (`
         <elsa-default-activity-template
                activity-type="${activityType}"
                display-text="${displayText}"
                display-type="${displayType}"
                activity="${activityJson}"
                can-start-workflow="${canStartWorkflow}" />
        `);
  }

}
