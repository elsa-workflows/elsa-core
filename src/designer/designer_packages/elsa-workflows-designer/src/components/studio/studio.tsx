import {Component, Element, EventEmitter, h, Host, Prop, Watch, Event} from '@stencil/core';
import 'reflect-metadata';
import {Container} from 'typedi';
import {AuthContext, EventBus, PluginRegistry, ServerSettings} from '../../services';
import {MonacoEditorSettings} from "../../services/monaco-editor-settings";
import {WorkflowDefinitionManager} from "../../modules/workflow-definitions/services/manager";
import {EventTypes} from "../../models";
import studioComponentStore from "../../data/studio-component-store";
import optionsStore from '../../data/designer-options-store';
import {StudioInitializingContext} from "../../models/studio";

@Component({
  tag: 'elsa-studio',
  styleUrl: 'studio.css',
})
export class Studio {
  private readonly eventBus: EventBus;
  private readonly workflowDefinitionManager: WorkflowDefinitionManager;
  private readonly pluginRegistry: PluginRegistry;

  constructor() {
    this.eventBus = Container.get(EventBus);
    this.workflowDefinitionManager = Container.get(WorkflowDefinitionManager);
    this.pluginRegistry = Container.get(PluginRegistry);
  }

  @Element() private el: HTMLElsaStudioElement;
  @Prop({attribute: 'server'}) serverUrl: string;
  @Prop({attribute: 'monaco-lib-path'}) monacoLibPath: string;
  @Prop({attribute: 'enable-flexible-ports'}) enableFlexiblePorts: boolean;
  @Prop({attribute: 'disable-auth'}) disableAuth: boolean;
  @Event() initializing: EventEmitter<StudioInitializingContext>;

  @Watch('serverUrl')
  private handleServerUrl(value: string) {
    const settings = Container.get(ServerSettings);
    settings.baseAddress = value;
  }

  @Watch('monacoLibPath')
  private handleMonacoLibPath(value: string) {
    const settings = Container.get(MonacoEditorSettings);
    settings.monacoLibPath = value;
  }

  async componentWillLoad() {
    const pluginRegistry = Container.get(PluginRegistry);
    const context: StudioInitializingContext = {container: Container, pluginRegistry};
    this.initializing.emit(context);

    this.handleMonacoLibPath(this.monacoLibPath);
    this.handleServerUrl(this.serverUrl);
    optionsStore.enableFlexiblePorts = this.enableFlexiblePorts;
    await this.eventBus.emit(EventTypes.Studio.Initializing, this);
    await this.pluginRegistry.initialize();

    // If we have a valid session, emit the signed in event so that descriptors will be loaded.
    const authContext = Container.get(AuthContext);

    if (this.disableAuth || authContext.getIsSignedIn()) {
      const eventBus = Container.get(EventBus);
      await eventBus.emit(EventTypes.Auth.SignedIn)
    }
  }

  render() {
    return <Host>
      <elsa-workflow-toolbar/>
      <div class="elsa-studio-content">
        {studioComponentStore.activeComponentFactory()}
      </div>
      {studioComponentStore.modalComponents.map(modal => modal())}
      <elsa-modal-dialog-container/>
    </Host>;
  }
}
