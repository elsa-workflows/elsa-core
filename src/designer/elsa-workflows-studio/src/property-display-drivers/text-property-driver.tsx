import {PropertyDisplayDriver} from "../services/property-display-driver";
import {ActivityModel, ActivityPropertyDescriptor} from "../models";
import {h} from "@stencil/core";
import {getProperty, setActivityDefinitionProperty, setActivityModelProperty} from "../utils/utils";

export class TextPropertyDriver implements PropertyDisplayDriver {
  display(activity: ActivityModel, property: ActivityPropertyDescriptor) {
    const key = `${activity.activityId}:${property.name}`;
    const fieldId = property.name;
    const fieldName = property.name;
    const fieldLabel = property.label || property.name;
    const fieldHint = property.hint;
    const prop = getProperty(activity.properties, property.name);
    const value = prop.expression;

    return (
      <div key={key} class="sm:col-span-6">
        <label htmlFor={fieldId} class="block text-sm font-medium text-gray-700">
          {fieldLabel}
        </label>
        <div class="mt-1">
          <input type="text" id={fieldId} name={fieldName} value={value} class="focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300"/>
        </div>
        {fieldHint ? <p class="mt-2 text-sm text-gray-500">{fieldHint}</p> : undefined}
      </div>
    )
  }

  update(activity: ActivityModel, property: ActivityPropertyDescriptor, form: FormData) {
    const value = form.get(property.name) as string;
    const syntax = 'Literal';
    setActivityModelProperty(activity, property.name, value, syntax);
  }
}
