import {TriggerDisplayContext, TriggerDriver} from "../../services/trigger-driver";

export class DefaultTriggerDriver implements TriggerDriver {
    display(context: TriggerDisplayContext): any {
        const triggerDescriptor = context.triggerDescriptor;
        const text = triggerDescriptor?.displayName;
        const borderColor = 'border-green-600';
        const backgroundColor = 'bg-green-400';
        const iconBackgroundColor = 'bg-green-500';

        return (`
          <div>
            <div class="activity-wrapper border ${borderColor} ${backgroundColor} rounded text-white overflow-hidden">
              <div class="flex flex-row">
                <div class="flex flex-shrink items-center ${iconBackgroundColor}">
                  <div class="px-2 py-1">
                    <svg class="h-6 w-6 text-white" width="24" height="24" viewBox="0 0 24 24"
                         stroke-width="2"
                         stroke="currentColor" fill="none" stroke-linecap="round" stroke-linejoin="round">
                      <path stroke="none" d="M0 0h24v24H0z"/>
                      <path d="M7 18a4.6 4.4 0 0 1 0 -9h0a5 4.5 0 0 1 11 2h1a3.5 3.5 0 0 1 0 7h-12"/>
                    </svg>
                  </div>
                </div>
                <div class="flex items-center">
                  <div class="px-4 py-1">
                    ${text}
                  </div>
                </div>
              </div>
            </div>
          </div>
        `);
    }

}
