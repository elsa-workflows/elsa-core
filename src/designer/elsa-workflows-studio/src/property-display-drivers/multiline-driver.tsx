import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h, State} from "@stencil/core";
import {getProperty, setActivityModelProperty} from "../utils/utils";

export class MultilineDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const key = `${activity.activityId}:${property.name}`;
    const prop = getProperty(activity.properties, property.name);

    return (
      <div class="sm:col-span-6">
        <elsa-text-property propertyDescriptor={property} propertyModel={prop} editor-height="8em"/>
      </div>
    )
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    const value = form.get(property.name) as string;
    const syntax = form.get(`${property.name}Syntax`) as string || 'Literal';
    setActivityModelProperty(activity, property.name, value, syntax);
  }
}
