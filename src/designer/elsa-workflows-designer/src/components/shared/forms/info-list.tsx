import {FunctionalComponent, h} from "@stencil/core";

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
            <h3 class="text-lg leading-6 font-medium text-gray-900">
              {title}
            </h3>
          </div>
          <div class="mt-5 border-t border-gray-200">
            <dl class="sm:divide-y sm:divide-gray-200">
              {Object.entries(dictionary).map(([k, v]) => (
                <div class="py-4 sm:py-5 sm:grid sm:grid-cols-3 sm:gap-4">
                  <dt class="text-sm font-medium text-gray-500">{k}</dt>
                  <dd class="mt-1 text-sm text-gray-900 sm:mt-0 sm:col-span-2">{v}</dd>
                </div>))}
            </dl>
          </div>
        </div>
      </div>
    </div>
  );
};
