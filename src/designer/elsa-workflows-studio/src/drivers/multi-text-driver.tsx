import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h} from "@stencil/core";
import {getOrCreateProperty} from "../utils/utils";

export class MultiTextDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor, onUpdated?: () => void) {
    const prop = getOrCreateProperty(activity, property.name);
    return <elsa-multi-text-property activityModel={activity} propertyDescriptor={property} propertyModel={prop} onValueChange={onUpdated}/>;
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
  }
}
