import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h} from "@stencil/core";
import {getProperty, setActivityModelProperty} from "../utils/utils";

export class DropdownDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const prop = getProperty(activity.properties, property.name);
    return <elsa-dropdown-property propertyDescriptor={property} propertyModel={prop}/>
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    const value = form.get(property.name) as string;
    setActivityModelProperty(activity, property.name, value, 'Literal');
  }
}
