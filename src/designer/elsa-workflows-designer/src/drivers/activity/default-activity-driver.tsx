import 'reflect-metadata';
import {h} from "@stencil/core";
import {Container, Service} from "typedi";
import {ActivityKind} from '../../models';
import {ActivityDisplayContext, ActivityDisplayType, ActivityDriver, ActivityIcon, ActivityIconRegistry} from '../../services';
import {isNullOrWhitespace} from "../../utils";

@Service()
export class DefaultActivityDriver implements ActivityDriver {
  private readonly iconRegistry: ActivityIconRegistry;

  constructor() {
    this.iconRegistry = Container.get(ActivityIconRegistry);
  }

  display(context: ActivityDisplayContext): any {
    const iconRegistry = this.iconRegistry;
    const activityDescriptor = context.activityDescriptor;
    const activityType = activityDescriptor.activityType;
    const activity = context.activity;
    const canStartWorkflow = activity?.canStartWorkflow;
    const text = activityDescriptor?.displayName;
    let displayText = activity?.metadata?.displayText;

    if(isNullOrWhitespace(displayText))
      displayText = text;

    const isTrigger = activityDescriptor?.kind == ActivityKind.Trigger;
    const borderColor = canStartWorkflow ? isTrigger ? 'border-green-600' : 'border-blue-600' : 'border-gray-300';
    const backgroundColor = canStartWorkflow ? isTrigger ? 'bg-green-400' : 'bg-blue-400' : 'bg-white';
    const textColor = canStartWorkflow ? 'text-white' : 'text-gray-700';
    const iconBackgroundColor = isTrigger ? 'bg-green-500' : 'bg-blue-500';
    const icon = iconRegistry.has(activityType) ? iconRegistry.get(activityType) : null;
    const displayTypeIsPicker = context.displayType == 'picker';
    const iconCssClass = displayTypeIsPicker ? 'px-2' : 'px-4';
    const cssClass = displayTypeIsPicker ? 'px-2 py-2' : 'px-4 py-4';

    const renderIcon = (icon?: ActivityIcon): string => {

      if (!icon)
        return ''; //return '<div class="px-1 py-1"><span></span></div>';

      return (
        `<div class="${iconCssClass} py-1">
        ${icon()}
      </div>`
      );
    }

    return (`
          <div>
            <div class="activity-wrapper border ${borderColor} ${backgroundColor} rounded text-white overflow-hidden">
              <div class="activity-content-wrapper flex flex-row">
                <div class="flex flex-shrink items-center ${iconBackgroundColor}">
                ${renderIcon(icon)}
                </div>
                <div class="flex items-center">
                  <div class="${cssClass}">
                    <span class=${textColor}>${displayText}</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        `);
  }

}
