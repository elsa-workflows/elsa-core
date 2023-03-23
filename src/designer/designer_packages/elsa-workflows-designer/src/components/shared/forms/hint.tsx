import {FunctionalComponent, h} from "@stencil/core";

export interface HintProps {
  text?: string
}

export const Hint: FunctionalComponent<HintProps> = ({text}) => text ? <p class="form-field-hint mt-2 text-sm text-gray-500">{text}</p> : undefined;
