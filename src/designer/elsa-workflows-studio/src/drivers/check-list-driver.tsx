import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h} from "@stencil/core";
import {getOrCreateProperty, setActivityModelProperty} from "../utils/utils";

export class CheckListDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const prop = getOrCreateProperty(activity, property.name);
    return <elsa-check-list-property propertyDescriptor={property} propertyModel={prop} />;
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
  }
}
