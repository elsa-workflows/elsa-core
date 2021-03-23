import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h} from "@stencil/core";
import {getProperty, setActivityModelProperty} from "../utils/utils";

export class MultiTextDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const prop = getProperty(activity.properties, property.name);
    return <elsa-multi-text-property propertyDescriptor={property} propertyModel={prop} />;
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    const value = form.get(property.name) as string;
    setActivityModelProperty(activity, property.name, value, 'Json');
  }
}
