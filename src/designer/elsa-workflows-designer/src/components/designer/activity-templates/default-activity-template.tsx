import {Component, h, Prop, State, Event, EventEmitter, Listen, Element} from "@stencil/core";
import {camelCase} from 'lodash';
import {ActivityIcon, ActivityIconRegistry} from "../../../services";
import {Container} from "typedi";
import {Activity, ActivityDescriptor, ActivityKind, ActivitySelectedArgs, Port} from "../../../models";
import descriptorsStore from "../../../data/descriptors-store";
import {isNullOrWhitespace} from "../../../utils";

@Component({
  tag: 'elsa-default-activity-template',
  shadow: false
})
export class DefaultActivityTemplate {
  private readonly iconRegistry: ActivityIconRegistry;
  private activityDescriptor: ActivityDescriptor;
  private parsedActivity: Activity;
  private icon: ActivityIcon;
  private portElements: Array<HTMLElement> = [];

  constructor() {
    this.iconRegistry = Container.get(ActivityIconRegistry);
  }

  @Prop({attribute: 'activity-type'}) activityType: string;
  @Prop({attribute: 'display-type'}) displayType: string;
  @Prop({attribute: 'activity'}) activityJson: string;
  @Prop() selected: boolean;
  @Prop() activity: Activity;
  @Event() activitySelected: EventEmitter<ActivitySelectedArgs>;
  @State() private selectedPortName: string;

  componentWillLoad() {
    const iconRegistry = this.iconRegistry;

    if (!!this.activity) {
      {
        this.parsedActivity = this.activity;
      }
    } else {
      const encodedActivityJson = this.activityJson;

      if (!isNullOrWhitespace(encodedActivityJson)) {
        const decodedActivityJson = decodeURI(encodedActivityJson);
        this.parsedActivity = JSON.parse(decodedActivityJson);
      }
    }

    this.activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.activityType == this.activityType);
    const activityType = this.activityType;
    this.icon = iconRegistry.has(activityType) ? iconRegistry.get(activityType) : null;
  }

  componentWillRender() {
    this.portElements = [];
  }

  render() {
    const activityDescriptor = this.activityDescriptor;
    const activity = this.parsedActivity;
    const canStartWorkflow = activity?.canStartWorkflow;
    const icon = this.icon;
    const textColor = canStartWorkflow ? 'text-white' : 'text-gray-700';
    const isTrigger = activityDescriptor?.kind == ActivityKind.Trigger;
    const backgroundColor = canStartWorkflow ? isTrigger ? 'bg-green-400' : 'bg-blue-400' : 'bg-white';
    const iconBackgroundColor = isTrigger ? 'bg-green-500' : 'bg-blue-500';
    const borderColor = this.selected ? 'border-blue-600' : canStartWorkflow ? isTrigger ? 'border-green-600' : 'border-blue-600' : 'border-gray-300';
    const displayTypeIsPicker = this.displayTypeIsPicker;
    const displayTypeIsEmbedded = this.displayTypeIsEmbedded;
    const containerCssClass = displayTypeIsEmbedded ? '' : 'drop-shadow-md';
    const contentCssClass = displayTypeIsPicker ? 'px-2 py-2' : 'px-4 py-4';
    let displayText = activity?.metadata?.displayText;

    if (isNullOrWhitespace(displayText))
      displayText = activityDescriptor?.displayName;

    return (
      <div>
        <div class={`activity-wrapper border ${borderColor} ${backgroundColor} ${containerCssClass} rounded text-white overflow-hidden`}>
          <div class="activity-content-wrapper flex flex-row">
            <div class={`flex flex-shrink items-center ${iconBackgroundColor}`}>
              {this.renderIcon(icon)}
            </div>
            <div class="flex items-center">
              <div class={contentCssClass}>
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
    const iconCssClass = this.displayTypeIsPicker ? 'px-2' : 'px-4';

    if (!icon)
      return undefined;

    return (
      <div class={`${iconCssClass} py-1`}>
        {icon()}
      </div>
    );
  }

  private renderPorts = () => {

    if (this.displayTypeIsPicker)
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
    const canStartWorkflow = this.parsedActivity?.canStartWorkflow;
    const textColor = canStartWorkflow ? 'text-white' : 'text-gray-700';
    const portName = camelCase(port.name);
    const activity = this.parsedActivity;
    const childActivity: Activity = activity ? activity[portName] : null;
    const isSelected = port.name == this.selectedPortName;

    return (
      <div class="activity-port" data-port-name={port.name} ref={el => this.portElements.push(el)}>
        <div>
          <span class={`${textColor} text-xs`}>{port.displayName}</span>
        </div>
        <div>
          {childActivity ? (
            <div class={`relative block w-full focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500`}
                 onMouseDown={e => this.onPortMouseDown(e, port)}
                 onMouseUp={e => this.onPortMouseUp(e, port)}
            >
              <elsa-default-activity-template activityType={childActivity.typeName} displayType="embedded" activity={childActivity} selected={isSelected}/>
              {/*<span class={textColor}>{childActivity.typeName}</span>*/}
            </div>
          ) : (
            <div onDragOver={this.onDragOverPort}
                 onDrop={this.onDropOnPort}
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

  private get displayTypeIsEmbedded(): boolean {
    return this.displayType == "embedded";
  }

  @Listen('click', {target: 'window'})
  private onWindowClicked(event: Event) {
    const target = event.target as HTMLElement;

    for (const portElement of this.portElements)
      if (portElement.contains(target))
        return;

    this.selectedPortName = null;
  }

  private onPortMouseDown = (e: MouseEvent, port: Port) => {
    e.stopPropagation();
  };

  private onPortMouseUp = (e: MouseEvent, port: Port) => {
    e.stopPropagation();

    if (this.selectedPortName != port.name) {
      this.selectedPortName = port.name;

      const activity = this.parsedActivity;
      const portName = camelCase(port.name);
      const childActivity: Activity = activity ? activity[portName] : null;

      const args: ActivitySelectedArgs = {
        activity: childActivity,
        applyChanges: a => {
          activity[portName] = a;
        },
        deleteActivity: a => {
          activity[portName] = null;
        }
      };

      this.activitySelected.emit(args);
    }
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
