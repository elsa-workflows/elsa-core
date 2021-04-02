import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h, State} from "@stencil/core";
import {getProperty, setActivityModelProperty} from "../utils/utils";

export class SingleLineDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const prop = getProperty(activity.properties, property.name);
    return (
      <elsa-property-editor propertyDescriptor={property} propertyModel={prop} editor-height="2em" single-line={true}>
        <elsa-text-property propertyDescriptor={property} propertyModel={prop}/>
      </elsa-property-editor>
    );
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    const value = form.get(property.name) as string;
    const syntax = form.get(`${property.name}Syntax`) as string || 'Literal';
    setActivityModelProperty(activity, property.name, value, syntax);
  }
}
