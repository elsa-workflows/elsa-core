import 'reflect-metadata';
import {Service} from "typedi";
import {ActivityDisplayContext, ActivityDriver} from '../services';

@Service()
export class DefaultActivityDriver implements ActivityDriver {

  display(context: ActivityDisplayContext): any {
    const activityDescriptor = context.activityDescriptor;
    const type = activityDescriptor.type;
    const version = activityDescriptor.version;
    const activity = context.activity;
    const activityId = activity?.activityId;
    const displayType = context.displayType;

    return (`<elsa-default-activity-template activity-type="${type}" activity-type-version="${version}" activity-id="${activityId}" display-type="${displayType}" />`);
  }
}
