import {FunctionalComponent, h} from '@stencil/core';
import {copyTextToClipboard} from '../../../utils';

export interface InfoListProps {
  title: string;
  dictionary?: any;
}

export const InfoList: FunctionalComponent<InfoListProps> = ({title, dictionary}) => {
  return (
    <div class="p-4">
      <div class="max-w-4xl mx-auto">
        <div>
          <div>
            <h3 class="text-lg leading-6 font-medium text-gray-900">{title}</h3>
          </div>
          <div class="mt-5 border-t border-gray-200">
            <dl class="sm:divide-y sm:divide-gray-200">
              {Object.entries(dictionary).map(([k, v]) => (
                <div class="py-4 sm:py-5 sm:grid sm:grid-cols-3 sm:gap-4">
                  <dt class="text-sm font-medium text-gray-500">{k}</dt>
                  <dd class="flex justify-between items-center mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                    {v}
                    <a href="#" class="ml-2 h-6 w-6 inline-block text-blue-500 hover:text-blue-300" title="Copy value">
                      <svg
                        onClick={() => copyTextToClipboard(v as string)}
                        width="24"
                        height="24"
                        viewBox="0 0 24 24"
                        stroke-width="2"
                        stroke="currentColor"
                        fill="none"
                        stroke-linecap="round"
                        stroke-linejoin="round"
                      >
                        <path stroke="none" d="M0 0h24v24H0z"/>
                        <rect x="8" y="8" width="12" height="12" rx="2"/>
                        <path d="M16 8v-2a2 2 0 0 0 -2 -2h-8a2 2 0 0 0 -2 2v8a2 2 0 0 0 2 2h2"/>
                      </svg>
                    </a>
                  </dd>
                </div>
              ))}
            </dl>
          </div>
        </div>
      </div>
    </div>
  );
};
