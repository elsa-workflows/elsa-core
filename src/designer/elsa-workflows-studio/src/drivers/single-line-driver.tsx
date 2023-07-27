import {PropertyDisplayDriver} from "../services";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h} from "@stencil/core";
import {getOrCreateProperty} from "../utils/utils";

export class SingleLineDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor, onUpdated?: () => void, isEncypted?: boolean) {
    const prop = getOrCreateProperty(activity, property.name);
    return <elsa-single-line-property activityModel={activity} propertyDescriptor={property} propertyModel={prop} isEncypted={isEncypted} />;
  }
}
