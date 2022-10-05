import { FieldDriver } from "../servicesCopy/field-driver";
import { Activity, ActivityPropertyDescriptor, RenderResult } from "../modelsCopy";
import { SelectItem } from "../components/field-editors/select-field/models";

export class SelectFieldDriver implements FieldDriver {
  displayEditor = (activity: Activity, property: ActivityPropertyDescriptor): RenderResult => {
    const name: string = property.name;
    const label: string = property.label;
    const value: string = activity.state[name] || '';
    const items: Array<SelectItem> = property.options.items || [];
    const itemsJson = encodeURI(JSON.stringify(items));

    return `<wf-select-field name="${ name }" label="${ label }" hint="${ property.hint }" data-items="${ itemsJson }" value="${ value }"></wf-select-field>`;
  };

  updateEditor = (activity: Activity, property: ActivityPropertyDescriptor, formData: FormData) => {
    const value = formData.get(property.name).toString();

    activity.state[property.name] = value.trim();
  };

}
