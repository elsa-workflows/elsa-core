import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor, IntellisenseContext} from "../models";
import {h} from "@stencil/core";
import {getOrCreateProperty, setActivityModelProperty} from "../utils/utils";

export class CodeEditorDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const prop = getOrCreateProperty(activity, property.name);
    const options = property.options || {};
    const editorHeight = this.getEditorHeight(options);
    const syntax = options.syntax;

    return <elsa-script-property activityModel={activity} propertyDescriptor={property} propertyModel={prop}
                                 editor-height={editorHeight} syntax={syntax}/>;
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    const value = form.get(property.name) as string;
    setActivityModelProperty(activity, property.name, value, "Literal");
  }

  getEditorHeight(options: any) {
    const editorHeightName = options.editorHeight || 'Default';

    switch (editorHeightName) {
      case 'Large':
        return '20em'
      case 'Default':
      default:
        return '8em';
    }
  }
}
