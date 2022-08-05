import {h} from "@stencil/core";

export const SecretIcon = props => {
  const color = props.color || 'sky';
  return (
    `<span class="${`elsa-rounded-lg elsa-inline-flex elsa-p-3 elsa-bg-${color}-50 elsa-text-${color}-700 elsa-ring-4 elsa-ring-white`}">
      <svg class="elsa-h-6 elsa-w-6" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
        <path stroke="none" d="M0 0h24v24H0z"/>  <polyline points="21 12 17 12 14 20 10 4 7 12 3 12"/>
      </svg>
    </span>`
  );
};