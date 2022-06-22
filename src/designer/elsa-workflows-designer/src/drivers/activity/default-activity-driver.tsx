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
    const displayType = context.displayType;
    const activityJson = displayType == 'designer' ? encodeURI(JSON.stringify(activity)) : '';

    return (`<elsa-default-activity-template activity-type="${activityType}" activity="${activityJson}" display-type="${displayType}" />`);
  }
}
