import { FieldDriver } from "../servicesCopy/field-driver";
import { Activity, ActivityPropertyDescriptor, RenderDesignerResult, RenderResult } from "../modelsCopy";

export class TextFieldDriver implements FieldDriver
{
  displayEditor = (activity: Activity, property: ActivityPropertyDescriptor): RenderResult => {
    const name = property.name;
    const label = property.label;
    const value = activity.state[name] || '';

    return `<wf-text-field name="${name}" label="${label}" hint="${property.hint}" value="${value}"></wf-text-field>`;
  };

  updateEditor = (activity: Activity, property: ActivityPropertyDescriptor, formData: FormData) => {
    activity.state[property.name] = formData.get(property.name).toString().trim();
  };

}
