// Supports hierarchical field names, e.g. 'foo.bar.baz`, which will map to e.g. { foo: { bar: { baz: ''} } }.
import {Event, h} from "@stencil/core";

export interface SelectOption {
  value: string;
  text: string;
}

export class FormContext {

  constructor(model: any, updater: (model: any) => any) {
    this.model = model;
    this.updater = updater;
  }

  public model: any;
  public updater: (model: any) => any;
}

export function textInput(context: FormContext, fieldName: string, label: string, value: string, hint?: string, fieldId?: string) {
  fieldId = fieldId || fieldName
  return (
    <div>
      <label htmlFor={fieldName} class="block text-sm font-medium text-gray-700">
        {label}
      </label>
      <div class="mt-1">
        <input type="text" id={fieldId} name={fieldName} value={value} onChange={e => onTextInputChange(e, context)} class="focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300"/>
      </div>
      {hint && hint.length > 0 ? <p class="mt-2 text-sm text-gray-500">{hint}</p> : undefined}
    </div>);
}

export function checkBox(context: FormContext, fieldName: string, label: string, checked: boolean, hint?: string, fieldId?: string) {
  fieldId = fieldId || fieldName
  return (
    <div class="relative flex items-start">
      <div class="flex items-center h-5">
        <input id={fieldId} name={fieldName} type="checkbox" value="true" checked={checked} onChange={e => onCheckBoxChange(e, context)} class="focus:ring-blue-500 h-4 w-4 text-blue-600 border-gray-300 rounded"/>
      </div>
      <div class="ml-3 text-sm">
        <label htmlFor={fieldId} class="font-medium text-gray-700">{label}</label>
        {hint && hint.length > 0 ? <p class="text-gray-500">{hint}</p> : undefined}
      </div>
    </div>);
}

export function textArea(context: FormContext, fieldName: string, label: string, value: string, hint?: string, fieldId?: string) {
  fieldId = fieldId || fieldName
  return (
    <div>
      <label htmlFor={fieldName} class="block text-sm font-medium text-gray-700">
        {label}
      </label>
      <div class="mt-1">
        <textarea id={fieldId} name={fieldName} value={value} onChange={e => onTextAreaChange(e, context)} rows={3} class="focus:ring-blue-500 focus:border-blue-500 block w-full min-w-0 rounded-md sm:text-sm border-gray-300"/>
      </div>
      {hint && hint.length > 0 ? <p class="mt-2 text-sm text-gray-500">{hint}</p> : undefined}
    </div>);
}

export function selectField(context: FormContext, fieldName: string, label: string, value: string, options: Array<SelectOption>, hint?: string, fieldId?: string) {
  fieldId = fieldId || fieldName
  return (
    <div>
      <label htmlFor={fieldName} class="block text-sm font-medium text-gray-700">
        {label}
      </label>
      <div class="mt-1">
        <select id={fieldId} name={fieldName} onChange={e => onSelectChange(e, context)} class="block focus:ring-blue-500 focus:border-blue-500 w-full shadow-sm sm:text-sm border-gray-300 rounded-md">
          {options.map(item => {
            const selected = item.value === value;
            return <option value={item.value} selected={selected}>{item.text}</option>;
          })}
        </select>
      </div>
      {hint && hint.length > 0 ? <p class="mt-2 text-sm text-gray-500">{hint}</p> : undefined}
    </div>);
}

export function updateField<T>(context: FormContext, fieldName: string, value: T) {
  const fieldNameHierarchy = fieldName.split('.');
  let clone = {...context.model};
  let current = clone;

  for (const name of fieldNameHierarchy.slice(0, fieldNameHierarchy.length - 1)) {
    if (!current[name])
      current[name] = {};

    current = current[name];
  }

  const leafFieldName = fieldNameHierarchy.last();
  current[leafFieldName] = value;

  context.model = clone;
  context.updater(clone);
}

export function onTextInputChange(e: Event, context: FormContext) {
  const element = e.target as HTMLInputElement;
  updateField(context, element.name, element.value.trim());
}

export function onTextAreaChange(e: Event, context: FormContext) {
  const element = e.target as HTMLTextAreaElement;
  updateField(context, element.name, element.value.trim());
}

export function onCheckBoxChange(e: Event, context: FormContext) {
  const element = e.target as HTMLInputElement;
  updateField(context, element.name, element.checked);
}

export function onSelectChange(e: Event, context: FormContext) {
  const element = e.target as HTMLSelectElement;
  updateField(context, element.name, element.value.trim());
}
