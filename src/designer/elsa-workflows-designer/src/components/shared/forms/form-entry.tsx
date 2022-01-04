import {FunctionalComponent, h} from "@stencil/core";
import {Hint} from "./hint";

export interface FormEntryProps {
  label: string;
  fieldId: string;
  key?: string;
  hint?: string;
}

export const FormEntry: FunctionalComponent<FormEntryProps> = ({label, hint, fieldId, key}, children) => {
  return (
    <div class="p-4">
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
