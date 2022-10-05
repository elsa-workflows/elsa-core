import { FieldDriver } from "../servicesCopy/field-driver";
import { Activity, ActivityPropertyDescriptor, RenderResult } from "../modelsCopy";

export class ListFieldDriver implements FieldDriver {
  displayEditor = (activity: Activity, property: ActivityPropertyDescriptor): RenderResult => {
    const name = property.name;
    const label = property.label;
    const items = activity.state[name] as Array<any> || [];
    const value = items.join(', ');

    return `<wf-list-field name="${ name }" label="${ label }" hint="${ property.hint }" items="${ value }"></wf-list-field>`;
  };

  updateEditor = (activity: Activity, property: ActivityPropertyDescriptor, formData: FormData) => {
    const value = formData.get(property.name).toString();

    activity.state[property.name] = value.split(',').map(x => x.trim());
  };

}
