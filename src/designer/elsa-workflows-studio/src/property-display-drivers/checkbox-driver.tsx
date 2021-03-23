import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h, State} from "@stencil/core";
import {getProperty, setActivityModelProperty} from "../utils/utils";

export class CheckboxDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const prop = getProperty(activity.properties, property.name);
    return <elsa-checkbox-property propertyDescriptor={property} propertyModel={prop} />;
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    const value = form.get(property.name) as string || 'false';
    setActivityModelProperty(activity, property.name, value, 'Literal');
  }
}
