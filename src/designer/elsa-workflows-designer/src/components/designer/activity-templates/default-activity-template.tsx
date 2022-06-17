import {Component, h, Prop, State} from "@stencil/core";
import {camelCase} from 'lodash';
import {ActivityIcon, ActivityIconRegistry} from "../../../services";
import {Container} from "typedi";
import {Activity, ActivityDescriptor, ActivityKind, Port} from "../../../models";
import descriptorsStore from "../../../data/descriptors-store";
import {isNullOrWhitespace} from "../../../utils";

@Component({
  tag: 'elsa-default-activity-template',
  shadow: false
})
export class DefaultActivityTemplate {
  private readonly iconRegistry: ActivityIconRegistry;
  private activityDescriptor: ActivityDescriptor;
  private activity: Activity;
  private icon: ActivityIcon;

  constructor() {
    this.iconRegistry = Container.get(ActivityIconRegistry);
  }

  @Prop({attribute: 'activity-type'}) activityType: string;
  @Prop({attribute: 'activity'}) activityJson: string;
  @Prop({attribute: 'display-text'}) displayText: string;
  @Prop({attribute: 'display-type'}) displayType: string;
  @Prop({attribute: 'can-start-workflow'}) canStartWorkflow: boolean;

  componentWillLoad() {
    const activityType = this.activityType;
    const iconRegistry = this.iconRegistry;
    const encodedActivityJson = this.activityJson;
    this.activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == activityType);

    if (!isNullOrWhitespace(encodedActivityJson)) {
      const decodedActivityJson = decodeURI(encodedActivityJson);
      this.activity = JSON.parse(decodedActivityJson);
    }

    this.icon = iconRegistry.has(activityType) ? iconRegistry.get(activityType) : null;
  }

  render() {
    const activityDescriptor = this.activityDescriptor;
    const canStartWorkflow = this.canStartWorkflow;
    const icon = this.icon;
    const textColor = canStartWorkflow ? 'text-white' : 'text-gray-700';
    const isTrigger = activityDescriptor?.kind == ActivityKind.Trigger;
    const backgroundColor = canStartWorkflow ? isTrigger ? 'bg-green-400' : 'bg-blue-400' : 'bg-white';
    const iconBackgroundColor = isTrigger ? 'bg-green-500' : 'bg-blue-500';
    const borderColor = canStartWorkflow ? isTrigger ? 'border-green-600' : 'border-blue-600' : 'border-gray-300';
    const displayTypeIsPicker = this.displayTypeIsPicker;
    const cssClass = displayTypeIsPicker ? 'px-2 py-2' : 'px-4 py-4';
    let displayText = this.displayText;

    return (
      <div>
        <div class={`activity-wrapper border ${borderColor} ${backgroundColor} rounded text-white overflow-hidden drop-shadow-md`}>
          <div class="activity-content-wrapper flex flex-row">
            <div class={`flex flex-shrink items-center ${iconBackgroundColor}`}>
              {this.renderIcon(icon)}
            </div>
            <div class="flex items-center">
              <div class={cssClass}>
                <span class={textColor}>{displayText}</span>
                <div>
                  {this.renderPorts()}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    )
  }

  private renderIcon = (icon?: ActivityIcon): string => {
    const displayTypeIsPicker = this.displayTypeIsPicker;
    const iconCssClass = displayTypeIsPicker ? 'px-2' : 'px-4';

    if (!icon)
      return undefined; //return '<div class="px-1 py-1"><span></span></div>';

    return (
      <div class={`${iconCssClass} py-1`}>
        {icon()}
      </div>
    );
  }

  private renderPorts = () => {

    if (this?.displayType != 'designer')
      return undefined;

    const activityDescriptor = this.activityDescriptor;
    const ports = activityDescriptor?.ports ?? [];

    if (ports.length == 0)
      return undefined;

    return (
      <div class="activity-ports mt-2 flex space-x-2">
        {ports.map(port => this.renderPort(port))}
      </div>
    );
  };

  private renderPort = (port: Port) => {
    const canStartWorkflow = this.canStartWorkflow;
    const textColor = canStartWorkflow ? 'text-white' : 'text-gray-700';
    const portName = camelCase(port.name);
    const activity = this.activity;
    const childActivity: Activity = activity ? activity[portName] : null;

    return (
      <div class="activity-port" data-port-name={port.name}>
        <div>
          <span class={`${textColor} text-xs`}>{port.displayName}</span>
        </div>
        <div>
          {childActivity ? (
            <div onDragOver={this.onDragOverPort}
                 onDrop={this.onDropOnPort}
                 onMouseDown={this.onPortMouseDown}
                 onMouseUp={this.onPortMouseUp}
                 class="relative block w-full border-2 border-gray-300 border-dashed rounded-lg p-6 text-center hover:border-gray-400 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
              <span class={textColor}>{childActivity.typeName}</span>
            </div>
          ) : (
            <div onDragOver={this.onDragOverPort}
                 onDrop={this.onDropOnPort}
                 onMouseDown={this.onPortMouseDown}
                 onMouseUp={this.onPortMouseUp}
                 class="relative block w-full border-2 border-gray-300 border-dashed rounded-lg p-6 text-center hover:border-gray-400 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500">
              <svg class="h-6 w-6 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4"/>
              </svg>
            </div>)}
        </div>
      </div>
    );
  };

  private get displayTypeIsPicker(): boolean {
    return this.displayType == "picker";
  }

  private onPortMouseDown = (e: MouseEvent) => {
    e.stopPropagation();
  };

  private onPortMouseUp = (e: MouseEvent) => {
    e.stopPropagation();
  };

  private onDragOverPort = (e: DragEvent) => {
    console.debug("Dragging over!");
    e.stopPropagation();
    e.preventDefault();
  }

  private onDropOnPort = (e: DragEvent) => {
    //debugger;
    console.debug("Dropped!");
  }
}
