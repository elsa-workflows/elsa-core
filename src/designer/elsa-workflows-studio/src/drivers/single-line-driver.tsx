import {PropertyDisplayDriver} from "../services";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h} from "@stencil/core";
import {getOrCreateProperty} from "../utils/utils";

export class SingleLineDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const prop = getOrCreateProperty(activity, property.name);
    return <elsa-single-line-property propertyDescriptor={property} propertyModel={prop}/>;
  }
}
