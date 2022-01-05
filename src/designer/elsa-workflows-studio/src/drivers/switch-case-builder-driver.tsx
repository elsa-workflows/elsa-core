import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h} from "@stencil/core";
import {getOrCreateProperty} from "../utils/utils";

export class SwitchCaseBuilderDriver implements PropertyDisplayDriver {

    display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
        const prop = getOrCreateProperty(activity, property.name);
        return <elsa-switch-cases-property activityModel={activity} propertyDescriptor={property} propertyModel={prop}/>
    }

    update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    }
}
