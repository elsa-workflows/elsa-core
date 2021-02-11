import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h} from "@stencil/core";

export class NullPropertyDriver implements PropertyDisplayDriver {
  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    return undefined;
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
  }
}
