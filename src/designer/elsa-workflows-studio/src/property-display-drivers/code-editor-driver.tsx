import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h, State} from "@stencil/core";
import {getProperty, setActivityModelProperty} from "../utils/utils";

export class CodeEditorDriver implements PropertyDisplayDriver {

  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const key = `${activity.activityId}:${property.name}`;
    const prop = getProperty(activity.properties, property.name);
    const options = property.options || {};
    const editorHeight = this.getEditorHeight(options);
    const context: string = options.context;
    const syntax = options.syntax;

    return (
      <div class="sm:col-span-6">
        <elsa-script-property propertyDescriptor={property} propertyModel={prop} editor-height={editorHeight} syntax={syntax} context={context}/>
      </div>
    );
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
