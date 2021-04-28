import {h} from "@stencil/core";

export const RedirectIcon = props =>
  (
    `<span class="${`rounded-lg inline-flex p-3 bg-light-blue-50 text-light-blue-700 ring-4 ring-white`}">
      <svg class="h-6 w-6" viewBox="0 0 24 24" stroke-width="2" stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
        <path stroke="none" d="M0 0h24v24H0z"/>
        <circle cx="6" cy="19" r="2"/>
        <circle cx="18" cy="5" r="2"/>
        <path d="M12 19h4.5a3.5 3.5 0 0 0 0 -7h-8a3.5 3.5 0 0 1 0 -7h3.5"/>
      </svg>
    </span>`
  );
