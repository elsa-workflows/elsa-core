import {h} from "@stencil/core";
import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {getOrCreateProperty} from "../utils/utils";

export class WorkflowDefinitionPropertyBuilderDriver implements PropertyDisplayDriver {

    display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
        const prop = getOrCreateProperty(activity, property.name);
        return <elsa-workflow-definition-property propertyDescriptor={property} propertyModel={prop}/>
    }

    update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    }
}