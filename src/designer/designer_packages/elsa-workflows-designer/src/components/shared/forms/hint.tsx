import {FunctionalComponent, h} from "@stencil/core";

export interface HintProps {
  text?: string
}

export const Hint: FunctionalComponent<HintProps> = ({text}) => text ? <p class="form-field-hint tw-mt-2 tw-text-sm tw-text-gray-500">{text}</p> : undefined;
