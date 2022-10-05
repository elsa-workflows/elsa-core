import {FunctionalComponent, h} from "@stencil/core";
import {Hint} from "./hint";

export interface FormEntryProps {
  label: string;
  fieldId: string;
  key?: string;
  hint?: string;
  padding?: string;
}

export const FormEntry: FunctionalComponent<FormEntryProps> = ({label, hint, fieldId, key, padding}, children) => {
  padding ??= 'p-4';
  return (
    <div class={padding}>
      <label htmlFor={fieldId}>
        {label}
      </label>
      <div class="mt-1" key={key}>
        {children}
      </div>
      <Hint text={hint}/>
    </div>
  );
};

export const CheckboxFormEntry: FunctionalComponent<FormEntryProps> = ({label, hint, fieldId, key, padding}, children) => {
  padding ??= 'p-4';
  return (
    <div class={padding}>
      <div class="flex space-x-1">
        {children}
        <label htmlFor={fieldId}>
          {label}
        </label>
      </div>
      <Hint text={hint}/>
    </div>
  );
};
