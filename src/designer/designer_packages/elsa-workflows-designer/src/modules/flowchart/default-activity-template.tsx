import {Component, h, Prop, State, Event, EventEmitter, Listen, Element} from "@stencil/core";
import {ActivityIconProducer, ActivityIconRegistry, PortProviderRegistry} from "../../services";
import {Container} from "typedi";
import {Activity, ActivityDescriptor, ActivityKind, ActivitySelectedArgs, ChildActivitySelectedArgs, EditChildActivityArgs, Port, PortType} from "../../models";
import descriptorsStore from "../../data/descriptors-store";
import {isNullOrWhitespace} from "../../utils";
import FlowchartTunnel from "./state";

@Component({
  tag: 'elsa-default-activity-template',
  shadow: false
})
export class DefaultActivityTemplate {
  private readonly iconRegistry: ActivityIconRegistry;
  private readonly portProviderRegistry: PortProviderRegistry;
  private activityDescriptor: ActivityDescriptor;
  private icon: ActivityIconProducer;
  private portElements: Array<HTMLElement> = [];

  constructor() {
    this.iconRegistry = Container.get(ActivityIconRegistry);
    this.portProviderRegistry = Container.get(PortProviderRegistry);
  }

  @Prop({attribute: 'activity-type'}) activityType: string;
  @Prop({attribute: 'activity-type-version'}) activityTypeVersion: number = 1;
  @Prop({attribute: 'display-type'}) displayType: string;
  @Prop({attribute: 'activity-id'}) activityId: string;
  @Event() editChildActivity: EventEmitter<EditChildActivityArgs>;
  @Event() childActivitySelected: EventEmitter<ChildActivitySelectedArgs>;
  @State() private selectedPortName: string;

  componentWillLoad() {
    const iconRegistry = this.iconRegistry;
    const activityType = this.activityType;
    const activityTypeVersion = this.activityTypeVersion ?? 0;
    this.activityDescriptor = descriptorsStore.activityDescriptors.find(x => x.typeName == activityType && x.version == activityTypeVersion);
    this.icon = iconRegistry.has(activityType) ? iconRegistry.get(activityType) : null;
  }

  componentWillRender() {
    this.portElements = [];
  }

  render() {
    const activityDescriptor = this.activityDescriptor;
    const activityId = this.activityId;
    const portProvider = this.portProviderRegistry.get(activityDescriptor.typeName);

    return (
      <FlowchartTunnel.Consumer>
        {({nodeMap}) => {
          const activity: Activity = nodeMap[activityId];
          const ports = portProvider.getOutboundPorts({activityDescriptor, activity});
          const embeddedPorts = ports.filter(x => x.type == PortType.Embedded && x.isBrowsable !== false);
          const canStartWorkflow: boolean = activity?.customProperties?.canStartWorkflow ?? activity?.customProperties?.CanStartWorkflow ?? activity?.canStartWorkflow ?? false;
          const icon = this.icon;
          const hasIcon = !!icon;
          const textColor = canStartWorkflow ? 'tw-text-white' : 'tw-text-gray-700';
          const isTrigger = activityDescriptor?.kind == ActivityKind.Trigger;
          const backgroundColor = canStartWorkflow ? isTrigger ? 'tw-bg-green-400' : 'tw-bg-blue-400' : 'tw-bg-white';
          const iconBackgroundColor = isTrigger ? 'tw-bg-green-500' : 'tw-bg-blue-500';
          const borderColor = canStartWorkflow ? isTrigger ? 'tw-border-green-600' : 'tw-border-blue-600' : 'tw-border-gray-300';
          const displayTypeIsPicker = this.displayTypeIsPicker;
          const displayTypeIsEmbedded = this.displayTypeIsEmbedded;
          const containerCssClass = displayTypeIsEmbedded ? '' : 'tw-drop-shadow-md';
          const contentCssClass = displayTypeIsPicker ? 'tw-px-2 tw-py-2' : 'tw-px-4 tw-pt-0 tw-pb-3';
          let displayText = activity?.metadata?.displayText;

          if (isNullOrWhitespace(displayText))
            displayText = activityDescriptor?.displayName;

          if (embeddedPorts.length == 0 || displayTypeIsPicker) {
            return (
              <div>
                <div class={`activity-wrapper tw-border ${borderColor} ${backgroundColor} ${containerCssClass} tw-rounded tw-text-white tw-overflow-hidden`}>
                  <div class="elsa-toolbar-menu-wrapper tw-flex tw-flex-row">
                    <div class={`tw-flex tw-flex-shrink tw-items-center ${iconBackgroundColor}`}>
                      {this.renderIcon(icon)}
                    </div>
                    <div class="tw-flex tw-items-center">
                      <div class={displayTypeIsPicker ? `tw-m-2` : 'tw-m-3'}>
                        <span class={`${textColor}`}>{displayText}</span>
                        <div>
                          {this.renderPorts(activity, embeddedPorts)}
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            );
          } else {
            return (
              <div>
                <div class={`activity-wrapper tw-border ${borderColor} ${backgroundColor} ${containerCssClass} tw-rounded tw-overflow-hidden`}>
                  <div class="tw-text-white">
                    <div class={`tw-flex tw-flex-shrink tw-items-center tw-py-3 ${ hasIcon ? 'tw-pr-3' : 'tw-px-3' } ${iconBackgroundColor}`}>
                      {this.renderIcon(icon)}
                      <span>{displayText}</span>
                    </div>
                  </div>
                  <div class="elsa-toolbar-menu-wrapper tw-flex tw-flex-col">
                    <div class="tw-flex tw-items-center">
                      <div class={contentCssClass}>
                        {this.renderPorts(activity, embeddedPorts)}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            );
          }
        }}
      </FlowchartTunnel.Consumer>
    )
  }

  private renderIcon = (icon?: ActivityIconProducer): string => {
    const iconCssClass = this.displayTypeIsPicker ? 'tw-px-2' : 'tw-px-4';

    if (!icon)
      return undefined;

    return (
      <div class={`${iconCssClass} tw-py-1`}>
        {icon()}
      </div>
    );
  }

  private renderPorts = (activity: Activity, embeddedPorts: Port[]) => {

    if (this.displayTypeIsPicker || !this.activityDescriptor)
      return;

    if (embeddedPorts.length == 0)
      return;

    return (
      <div class="activity-ports tw-mt-2 tw-flex tw-space-x-2">
        {embeddedPorts.map(port => this.renderPort(activity, port))}
      </div>
    );
  };

  private renderPort = (activity: Activity, port: Port) => {
    const canStartWorkflow = activity?.canStartWorkflow == true;
    const displayTextClass = canStartWorkflow ? 'tw-text-white' : 'tw-text-gray-600';
    const borderColor = port.name == this.selectedPortName ? 'tw-border-blue-600' : 'tw-border-gray-300';
    const activityDescriptor = this.activityDescriptor;
    const portProvider = this.portProviderRegistry.get(activityDescriptor.typeName);
    const activityProperty = portProvider.resolvePort(port.name, {activity, activityDescriptor}) as Activity;

    const renderActivityProperty = () => {

      if (!activityProperty) {
        return (
          <div class="tw-relative tw-block tw-w-full tw-border-2 tw-border-gray-300 tw-border-dashed tw-rounded-lg tw-p-3 tw-text-center focus:tw-outline-none">
            <a href="#"
               onClick={e => this.onEditChildActivityClick(e, activity, port)}
               onMouseDown={e => e.stopPropagation()}
               class="tw-text-gray-400 hover:tw-text-gray-600">
              <div class="tw-flex-grow">
                <span class={`tw-text-sm ${displayTextClass}`}>{port.displayName}</span>
              </div>
            </a>
          </div>
        );
      }

      const propertyIsArray = Array.isArray(activityProperty);

      if (!propertyIsArray) {
        return (
          <div class={`tw-relative tw-block tw-w-full tw-border-2 ${borderColor} tw-border-solid tw-rounded-lg tw-p-3 tw-text-center focus:tw-outline-none`}>
            <div class="tw-flex tw-space-x-2">
              <a href="#"
                 onClick={e => this.onEditChildActivityClick(e, activity, port)}
                 onMouseDown={e => e.stopPropagation()}>
                <div class="tw-flex-grow">
                  <span class={`tw-text-sm ${displayTextClass}`}>{port.displayName}</span>
                </div>
              </a>
            </div>
          </div>
        );
      }

      return (
        <div class={`tw-relative tw-block tw-w-full tw-border-2 ${borderColor} tw-border-solid tw-rounded-lg tw-p-5 tw-text-center focus:tw-outline-none`}>
          <div class="tw-flex tw-space-x-2">
            <a href="#"
               onClick={e => this.onEditChildActivityClick(e, activity, port)}
               onMouseDown={e => e.stopPropagation()}>
              <div class="tw-flex-grow">
                <span class={`tw-text-sm ${displayTextClass}`}>{port.displayName}</span>
              </div>
            </a>
          </div>
        </div>
      );
    }

    return (
      <div class="activity-port" data-port-name={port.name} ref={el => this.portElements.push(el)}>
        <div>
          {renderActivityProperty()}
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

  private onEditChildActivityClick = (e: MouseEvent, parentActivity: Activity, port: Port) => {
    e.preventDefault();
    this.editChildActivity.emit({parentActivityId: parentActivity.id, port: port});
  };
}
