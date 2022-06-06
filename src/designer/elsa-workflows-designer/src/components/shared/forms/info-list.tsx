import { FunctionalComponent, h } from '@stencil/core';
import { isNullOrWhitespace } from '../../../utils';

export interface InfoListProps {
  title: string;
  dictionary?: { [key: string]: string };
  hideEmptyValues?: boolean;
}

export const InfoList: FunctionalComponent<InfoListProps> = ({ title, dictionary, hideEmptyValues }) => {
  let entries = Object.entries(dictionary);

  if (hideEmptyValues) entries = entries.filter(([k, v]) => !isNullOrWhitespace(v));

  return (
    <div class="p-4">
      <div class="mx-auto">
        <div>
          <div>
            <h3 class="text-lg leading-6 font-medium text-gray-900">{title}</h3>
          </div>
          <div class="mt-5 border-t border-gray-200">
            <dl class="sm:divide-y sm:divide-gray-200">
              {entries.map(([k, v]) => (
                <div class="py-4 sm:py-5 sm:grid sm:grid-cols-3 sm:gap-4">
                  <dt class="text-sm font-medium text-gray-500">{k}</dt>
                  <dd class="flex justify-between items-center mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">
                    {v}
                    {isNullOrWhitespace(v) ? undefined : <elsa-copy-button value={v} />}
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
