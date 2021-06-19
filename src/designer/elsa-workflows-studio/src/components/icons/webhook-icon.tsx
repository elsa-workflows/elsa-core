import {h} from "@stencil/core";

export const WebhookIcon = props =>
  (
    `<span class="${`elsa-rounded-lg elsa-inline-flex elsa-p-3 elsa-bg-rose-50 elsa-text-rose-700 elsa-ring-4 elsa-ring-white`}">
      <svg class="elsa-h-6 elsa-w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width={2} d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z"/>
      </svg>
    </span>`
  );
