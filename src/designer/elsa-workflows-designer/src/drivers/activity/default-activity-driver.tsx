import 'reflect-metadata';
import {h} from "@stencil/core";
import {Container, Service} from "typedi";
import {ActivityDisplayContext, ActivityDriver} from '../../services';

@Service()
export class DefaultActivityDriver implements ActivityDriver {

  display(context: ActivityDisplayContext): any {
    const activityDescriptor = context.activityDescriptor;
    const typeName = activityDescriptor.typeName;
    const version = activityDescriptor.version;
    const activity = context.activity;
    const activityId = activity?.id;
    const displayType = context.displayType;

    return (`<elsa-default-activity-template activity-type="${typeName}" activity-type-version="${version}" activity-id="${activityId}" display-type="${displayType}" />`);
  }
}
