import { FieldDriver } from "../servicesCopy/field-driver";
import { Activity, ActivityPropertyDescriptor, RenderResult } from "../modelsCopy";

export class BooleanFieldDriver implements FieldDriver
{
  displayEditor = (activity: Activity, property: ActivityPropertyDescriptor): RenderResult => {
    const name = property.name;
    const label = property.label;
    const checked = activity.state[name] === 'true';

    return `<wf-boolean-field name="${name}" label="${label}" hint="${property.hint}" checked="${checked}"></wf-boolean-field>`;
  };

  updateEditor = (activity: Activity, property: ActivityPropertyDescriptor, formData: FormData) => {
    activity.state[property.name] = formData.get(property.name);
  };

}
