import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h} from "@stencil/core";
import {getOrCreateProperty, setActivityModelProperty} from "../utils/utils";

export class DropdownDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const prop = getOrCreateProperty(activity, property.name);
    return <elsa-dropdown-property activityModel={activity} propertyDescriptor={property} propertyModel={prop}/>
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
  }
}
