import {h} from '@stencil/core';
import {createElsaWorkflowSettingsClient, SaveWorkflowSettingsRequest} from "../services/elsa-client";
import {eventBus, ElsaPlugin} from "../../../services";
import {
  EventTypes,
  ConfigureWorkflowRegistryColumnsContext,
  ConfigureWorkflowRegistryUpdatingContext,
  ElsaStudio
} from "../../../models";
import {cloneDeep} from 'lodash';

import { WorkflowSettingsRenderProps } from '../../../components/screens/workflow-definition-editor/elsa-workflow-settings-modal/elsa-workflow-settings-modal';
import { WorkflowSettings } from '../models';

export class WorkflowSettingsPlugin implements ElsaPlugin {
  serverUrl: string = 'https://localhost:11000';

  constructor(elsaStudio: ElsaStudio) {
    this.serverUrl = elsaStudio.serverUrl;

    eventBus.on(EventTypes.WorkflowRegistryLoadingColumns, this.onLoadingColumns);
    eventBus.on(EventTypes.WorkflowRegistryUpdating, this.onUpdating);
    eventBus.on(EventTypes.WorkflowSettingsUpdaing, this.onSettingsUpdating);
    eventBus.on(EventTypes.WorkflowSettingsBulkDelete, this.onBulkDelete);
    eventBus.on(EventTypes.WorkflowSettingsModalLoaded, this.onModalLoaded);
  }

  onModalLoaded(renderProps: WorkflowSettingsRenderProps) {
    const tabs = renderProps.tabs;

    const renderPropertiesTab = () => {
      return ( <elsa-workflow-definition-properties-tab properties={renderProps.properties}
                  workflowDefinitionId={renderProps.workflowDefinition.definitionId}  
                  onPropertiesToRemoveChanged={(e) => renderProps.propertiesToRemove = e.detail} 
                  onPropertiesChanged={(e) => renderProps.workflowDefinition.settings = e.detail}
                  onFormValidationChanged={e => { 
                    eventBus.emit(EventTypes.WorkflowPropertiesValidationChanged, this, e.detail);
                  }}/> )
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

  async onSettingsUpdating(settings: WorkflowSettings[]) {
    if(!settings) {
      return;
    }
    
    const elsaClient = await createElsaWorkflowSettingsClient(this.serverUrl);

    await elsaClient.workflowSettingsApi.saveAll(settings);
  }

  async onBulkDelete(ids: Array<string>) {
    if(!ids || !ids.length) {
      return;
    }

    const elsaClient = await createElsaWorkflowSettingsClient(this.serverUrl);
    await elsaClient.workflowSettingsApi.bulkDelete(ids);

    eventBus.emit(EventTypes.WorkflowSettingsDeleted, this);
  }
}
