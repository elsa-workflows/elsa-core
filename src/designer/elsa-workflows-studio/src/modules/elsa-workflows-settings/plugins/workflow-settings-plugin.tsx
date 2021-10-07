import {h} from '@stencil/core';
import {createElsaWorkflowSettingsClient, SaveWorkflowSettingsRequest} from "../services/elsa-client";
import {eventBus, ElsaPlugin} from "../../../services";
import {
  EventTypes,
  ConfigureWorkflowRegistryColumnsContext,
  ConfigureWorkflowRegistryUpdatingContext,
  ElsaStudio
} from "../../../models";

import { ActivityModel, ActivityPropertyDescriptor } from "../../..";

export class WorkflowSettingsPlugin implements ElsaPlugin {
  serverUrl: string;

  constructor(elsaStudio: ElsaStudio) {
    this.serverUrl = elsaStudio.serverUrl;

    eventBus.on(EventTypes.WorkflowRegistryLoadingColumns, this.onLoadingColumns);
    eventBus.on(EventTypes.WorkflowRegistryUpdating, this.onUpdating);
    eventBus.on(EventTypes.WorkflowSettingsModalLoaded, this.onModalLoaded)
  }

  onModalLoaded(renderProps: any) {
    const tabs = renderProps.tabs;

    const activityModel: ActivityModel = {
      type: '',
      activityId: '',
      outcomes: [],
      properties: [],
      propertyStorageProviders: {}
    };
    const propertyDescriptor: ActivityPropertyDescriptor = {
      defaultSyntax: "WorkflowDefinitionProperty",
      disableWorkflowProviderSelection: false,
      hint: "The conditions to evaluate.",
      isReadOnly: false,
      label: "Property",
      name: "Property",
      supportedSyntaxes: [],
      uiHint: "workflow-definition-property-builder",
    } 

    Object.assign(renderProps, { activityModel, propertyDescriptor})

    const renderPropertiesTab = () => {
      return ( <elsa-workflow-settings-properties-tab activityModel={activityModel} propertyDescriptor={propertyDescriptor} /> )
    }

    tabs.push({
      tabName: 'Properties',
      renderContent: renderPropertiesTab
    })
  }

  onLoadingColumns(context: ConfigureWorkflowRegistryColumnsContext) {
    const headers: any[] = [["Enabled"]];
    const hasContextItems: boolean = true;

    context.data = {headers, hasContextItems};
  }

  async onUpdating(context: ConfigureWorkflowRegistryUpdatingContext) {

    const elsaClient = await createElsaWorkflowSettingsClient(this.serverUrl);
    const workflowSettings = await elsaClient.workflowSettingsApi.list();

    const workflowBlueprintSettings = workflowSettings.find(x => x.workflowBlueprintId == context.params[0] && x.key == context.params[1]);
    if (workflowBlueprintSettings != undefined)
      await elsaClient.workflowSettingsApi.delete(workflowBlueprintSettings.id);

    const request: SaveWorkflowSettingsRequest = {
      workflowBlueprintId: context.params[0],
      key: context.params[1],
      value: context.params[2]
    };

    await elsaClient.workflowSettingsApi.save(request);
    await eventBus.emit(EventTypes.WorkflowRegistryUpdated, this);
  }
}
